#pragma warning disable OPENAI001 // OpenAI chat APIs are marked experimental.
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenAI.Chat;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Tags.Services;
using PerfumeTracker.Server.Features.YearInReview;

namespace PerfumeTracker.xTests.Tests;

// Offline coverage for the OpenAI ChatClient-backed identification services. The real client is
// replaced with a Moq mock fed canned ChatCompletion payloads (parsed via ModelReaderWriter), so
// the caching, parsing and guard branches run without any network or database access.
public static class ChatCompletionCanned {
	// Content text is JsonSerializer-serialized before embedding so quotes stay valid JSON.
	public static ChatCompletion Completion(string content) {
		var payload = JsonSerializer.Serialize(content);
		var json = $$$"""
			{"id":"chatcmpl_test","object":"chat.completion","created":1700000000,"model":"gpt-5",
			 "choices":[{"index":0,"message":{"role":"assistant","content":{{{payload}}},"refusal":null},"finish_reason":"stop"}],
			 "usage":{"prompt_tokens":10,"completion_tokens":20,"total_tokens":30}}
			""";
		return ModelReaderWriter.Read<ChatCompletion>(BinaryData.FromString(json))!;
	}

	// An assistant message with empty text exercises the empty-content guards (an empty choices
	// array would make ChatCompletion.Content itself throw before the service guards run).
	public static ChatCompletion EmptyCompletion() {
		const string json = """
			{"id":"chatcmpl_empty","object":"chat.completion","created":1700000000,"model":"gpt-5",
			 "choices":[{"index":0,"message":{"role":"assistant","content":""},"finish_reason":"stop"}],
			 "usage":{"prompt_tokens":1,"completion_tokens":0,"total_tokens":1}}
			""";
		return ModelReaderWriter.Read<ChatCompletion>(BinaryData.FromString(json))!;
	}

	public static Mock<ChatClient> ClientReturning(ChatCompletion completion) {
		var mock = new Mock<ChatClient>("gpt-test", "fake-key");
		_ = mock.Setup(c => c.CompleteChatAsync(It.IsAny<IEnumerable<OpenAI.Chat.ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ClientResult.FromValue(completion, new Mock<PipelineResponse>().Object));
		return mock;
	}

	public static Mock<ChatClient> ClientThrowing(Exception exception) {
		var mock = new Mock<ChatClient>("gpt-test", "fake-key");
		_ = mock.Setup(c => c.CompleteChatAsync(It.IsAny<IEnumerable<OpenAI.Chat.ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(exception);
		return mock;
	}

	public static void VerifyCompletionOnce(Mock<ChatClient> mock) =>
		mock.Verify(c => c.CompleteChatAsync(It.IsAny<IEnumerable<OpenAI.Chat.ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()), Times.Once);

	public static void VerifyNeverCalled(Mock<ChatClient> mock) =>
		mock.Verify(c => c.CompleteChatAsync(It.IsAny<IEnumerable<OpenAI.Chat.ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()), Times.Never);
}

public class PerfumeIdentifierTests {
	[Fact]
	public async Task GetIdentifiedPerfumeAsync_CacheMiss_ParsesCompletionAndCachesIt() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"house":"Chanel","perfumeName":"Bleu de Chanel","family":"Woody","notes":["Sandalwood","Citrus"],"confidenceScore":0.9}"""));
		var identifier = new PerfumeIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedPerfumeAsync("Chanel", "Bleu de Chanel", userId, TestContext.Current.CancellationToken);

		Assert.Equal("Chanel", result.House);
		Assert.Equal("Bleu de Chanel", result.PerfumeName);
		Assert.Equal("Woody", result.Family);
		Assert.Equal(["Sandalwood", "Citrus"], result.Notes);
		Assert.Equal(0.9, result.ConfidenceScore);
		ChatCompletionCanned.VerifyCompletionOnce(client);
		Assert.True(cache.TryGetValue<string>($"completion:identify-perfume:{userId}:Chanel::Bleu de Chanel", out var stored));
		Assert.Contains("confidenceScore", stored!);
	}

	[Fact]
	public async Task GetIdentifiedPerfumeAsync_CacheHit_ReusesStoredTextWithoutCallingOpenAI() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-perfume:{userId}:Diptyque::Philosykos",
			"""{"house":"Diptyque","perfumeName":"Philosykos","family":"Green","notes":["Fig"],"confidenceScore":0.7}""");
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		var identifier = new PerfumeIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedPerfumeAsync("Diptyque", "Philosykos", userId, TestContext.Current.CancellationToken);

		Assert.Equal("Green", result.Family);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task GetIdentifiedPerfumeAsync_CacheKeyIsScopedPerUser() {
		var owner = Guid.NewGuid();
		var other = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-perfume:{owner}:Chanel::Bleu de Chanel",
			"""{"house":"Chanel","perfumeName":"Bleu de Chanel","family":"Cached","notes":[],"confidenceScore":1.0}""");
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"house":"Chanel","perfumeName":"Bleu de Chanel","family":"Fresh","notes":[],"confidenceScore":0.5}"""));
		var identifier = new PerfumeIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedPerfumeAsync("Chanel", "Bleu de Chanel", other, TestContext.Current.CancellationToken);

		Assert.Equal("Fresh", result.Family);
		ChatCompletionCanned.VerifyCompletionOnce(client);
	}

	[Fact]
	public async Task GetIdentifiedPerfumeAsync_NullJsonPayload_Throws() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion("null"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new PerfumeIdentifier(client.Object, cache);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			identifier.GetIdentifiedPerfumeAsync("Acme", "Mystery", Guid.NewGuid(), TestContext.Current.CancellationToken));
		Assert.Equal("Failed to parse OpenAI response", ex.Message);
	}

	[Fact]
	public async Task GetIdentifiedPerfumeAsync_OpenAiThrows_Propagates() {
		var client = ChatCompletionCanned.ClientThrowing(new HttpRequestException("upstream down"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new PerfumeIdentifier(client.Object, cache);

		await Assert.ThrowsAsync<HttpRequestException>(() =>
			identifier.GetIdentifiedPerfumeAsync("Acme", "Mystery", Guid.NewGuid(), TestContext.Current.CancellationToken));
	}
}

public class ParfumeurIdentifierTests {
	[Fact]
	public async Task GetIdentifiedParfumeurAsync_CacheMiss_ParsesCompletionAndCachesIt() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"house":"Chanel","perfumeName":"Bleu de Chanel","parfumeur":"Jacques Polge","confidenceScore":0.85}"""));
		var identifier = new ParfumeurIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedParfumeurAsync("Chanel", "Bleu de Chanel", userId, TestContext.Current.CancellationToken);

		Assert.Equal("Jacques Polge", result.Parfumeur);
		Assert.Equal(0.85, result.ConfidenceScore);
		ChatCompletionCanned.VerifyCompletionOnce(client);
		Assert.True(cache.TryGetValue<string>($"completion:identify-parfumeur:{userId}:Chanel::Bleu de Chanel", out var stored));
		Assert.Contains("Jacques Polge", stored!);
	}

	[Fact]
	public async Task GetIdentifiedParfumeurAsync_CacheHit_ReusesStoredTextWithoutCallingOpenAI() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-parfumeur:{userId}:Guerlain::Shalimar",
			"""{"house":"Guerlain","perfumeName":"Shalimar","parfumeur":"Jacques Guerlain","confidenceScore":0.9}""");
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		var identifier = new ParfumeurIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedParfumeurAsync("Guerlain", "Shalimar", userId, TestContext.Current.CancellationToken);

		Assert.Equal("Jacques Guerlain", result.Parfumeur);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task GetIdentifiedParfumeurAsync_NullJsonPayload_Throws() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion("null"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new ParfumeurIdentifier(client.Object, cache);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			identifier.GetIdentifiedParfumeurAsync("Acme", "Mystery", Guid.NewGuid(), TestContext.Current.CancellationToken));
		Assert.Equal("Failed to parse OpenAI response", ex.Message);
	}

	[Fact]
	public async Task GetIdentifiedParfumeurAsync_EmptyCompletionContent_Throws() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.EmptyCompletion());
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new ParfumeurIdentifier(client.Object, cache);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			identifier.GetIdentifiedParfumeurAsync("Acme", "Mystery", Guid.NewGuid(), TestContext.Current.CancellationToken));
		Assert.Equal("OpenAI returned an empty or invalid response", ex.Message);
	}
}

public class TagNoteGroupIdentifierTests {
	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_CacheMiss_GroupsTagsAndCachesEachOne() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"Tags":[{"TagName":"Bergamot","NoteGroup":"Citrus","ConfidenceScore":0.95},{"TagName":"Sandalwood","NoteGroup":"Woody","ConfidenceScore":0.9}]}"""));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["Bergamot", "Sandalwood"], userId, TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Tags.Count());
		Assert.Equal("Citrus", result.Tags.Single(t => t.TagName == "Bergamot").NoteGroup);
		ChatCompletionCanned.VerifyCompletionOnce(client);
		Assert.True(cache.TryGetValue<string>($"completion:identify-note-group:{userId}:note-group::bergamot", out var bergamot));
		Assert.Contains("Citrus", bergamot!);
		Assert.True(cache.TryGetValue<string>($"completion:identify-note-group:{userId}:note-group::sandalwood", out var sandalwood));
		Assert.Contains("Woody", sandalwood!);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_MixedCacheOnlyOneOpenAiCallForUncachedTags() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-note-group:{userId}:note-group::bergamot",
			"""{"TagName":"Bergamot","NoteGroup":"Citrus","ConfidenceScore":0.9}""");
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"Tags":[{"TagName":"Rose","NoteGroup":"Floral","ConfidenceScore":0.88}]}"""));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["Bergamot", "Rose"], userId, TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Tags.Count());
		Assert.Equal("Citrus", result.Tags.Single(t => t.TagName == "Bergamot").NoteGroup);
		Assert.Equal("Floral", result.Tags.Single(t => t.TagName == "Rose").NoteGroup);
		ChatCompletionCanned.VerifyCompletionOnce(client);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_AllTagsCached_SkipsOpenAiEntirely() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-note-group:{userId}:note-group::rose",
			"""{"TagName":"Rose","NoteGroup":"Floral","ConfidenceScore":0.8}""");
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["Rose"], userId, TestContext.Current.CancellationToken);

		Assert.Equal("Floral", Assert.Single(result.Tags).NoteGroup);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_CacheKeysAreCaseAndWhitespaceInsensitive() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-note-group:{userId}:note-group::bergamot",
			"""{"TagName":"Bergamot","NoteGroup":"Citrus","ConfidenceScore":0.9}""");
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		// " Bergamot" and "BERGAMOT" collapse to one distinct tag whose lowercase key hits the cache.
		var result = await identifier.GetIdentifiedTagNoteGroupsAsync([" Bergamot", "BERGAMOT"], userId, TestContext.Current.CancellationToken);

		Assert.Equal("Citrus", Assert.Single(result.Tags).NoteGroup);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_NoUsableTagNames_ReturnsEmptyWithoutCallingOpenAI() {
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["", "   "], Guid.NewGuid(), TestContext.Current.CancellationToken);

		Assert.Empty(result.Tags);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_ResponseTagNotInInputs_IsIgnored() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"Tags":[{"TagName":"Bergamot","NoteGroup":"Citrus","ConfidenceScore":0.9},{"TagName":"Invented","NoteGroup":"Citrus","ConfidenceScore":0.5}]}"""));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["Bergamot"], userId, TestContext.Current.CancellationToken);

		Assert.Equal("Bergamot", Assert.Single(result.Tags).TagName);
		Assert.False(cache.TryGetValue<string>($"completion:identify-note-group:{userId}:note-group::invented", out _));
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_CorruptedCachedEntry_IsTreatedAsUncached() {
		var userId = Guid.NewGuid();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set($"completion:identify-note-group:{userId}:note-group::rose", "   ");
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			"""{"Tags":[{"TagName":"Rose","NoteGroup":"Floral","ConfidenceScore":0.7}]}"""));
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var result = await identifier.GetIdentifiedTagNoteGroupsAsync(["Rose"], userId, TestContext.Current.CancellationToken);

		Assert.Equal("Floral", Assert.Single(result.Tags).NoteGroup);
		ChatCompletionCanned.VerifyCompletionOnce(client);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_NullJsonPayload_Throws() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion("null"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			identifier.GetIdentifiedTagNoteGroupsAsync(["Rose"], Guid.NewGuid(), TestContext.Current.CancellationToken));
		Assert.Equal("Failed to parse OpenAI note group response", ex.Message);
	}

	[Fact]
	public async Task GetIdentifiedTagNoteGroupsAsync_EmptyCompletionContent_Throws() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.EmptyCompletion());
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var identifier = new TagNoteGroupIdentifier(client.Object, cache);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			identifier.GetIdentifiedTagNoteGroupsAsync(["Rose"], Guid.NewGuid(), TestContext.Current.CancellationToken));
		Assert.Equal("OpenAI returned an empty or invalid note group response", ex.Message);
	}
}

public class YearInReviewAiTests {
	private static List<YearInReviewAiCandidate> BuildCandidates() => [
		new YearInReviewAiCandidate(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Chanel", "Bleu de Chanel", "Woody", 42, 4.5m,
			[new YearInReviewAiTag(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Citrus", "Bright top notes")], ["Most worn"]),
		new YearInReviewAiCandidate(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Maison Margiela", "By the Fireplace", "Gourmand", 12, 4.0m,
			[], ["Unusual"]),
	];

	// Mirrors the fingerprint the service builds so tests can pre-seed or assert its cache entry.
	private static string CacheKey(int year, IReadOnlyList<YearInReviewAiCandidate> candidates) {
		var fingerprint = string.Join('|', candidates.Select(x =>
			$"{x.PerfumeId:N}:{x.YearWears}:{x.Rating}:{string.Join(',', x.Tags.Select(t => t.TagId.ToString("N")))}:{string.Join(',', x.CandidateReasons)}"));
		return $"year-review-ai:v4:{year}:{fingerprint}";
	}

	private static YearInReviewAi BuildService(Mock<ChatClient> client, IMemoryCache cache) =>
		new(client.Object, cache, NullLogger<YearInReviewAi>.Instance);

	[Fact]
	public async Task SelectCategories_NoCandidates_ReturnsNullWithoutCallingOpenAI() {
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var service = BuildService(client, cache);

		var result = await service.SelectCategories(2025, [], TestContext.Current.CancellationToken);

		Assert.Null(result);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task SelectCategories_SelectsCategoriesAndCachesResultForSecondCall() {
		var candidates = BuildCandidates();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var crowdPleaser = Guid.Parse("11111111-1111-1111-1111-111111111111");
		var wildcard = Guid.Parse("33333333-3333-3333-3333-333333333333");
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion(
			$$$"""
			{"crowdPleaserPerfumeId":"{{{crowdPleaser}}}","crowdPleaserReason":"A modern classic.",
			 "wildcardPerfumeId":"{{{wildcard}}}","wildcardReason":"Smells like burning logs.",
			 "comfortScentPerfumeId":"{{{wildcard}}}","comfortScentReason":"Cosy and smoky."}
			"""));
		var service = BuildService(client, cache);

		var first = await service.SelectCategories(2025, candidates, TestContext.Current.CancellationToken);
		var second = await service.SelectCategories(2025, candidates, TestContext.Current.CancellationToken);

		Assert.NotNull(first);
		Assert.Equal(crowdPleaser, first.CrowdPleaserPerfumeId);
		Assert.Equal("A modern classic.", first.CrowdPleaserReason);
		Assert.NotNull(second);
		Assert.Equal(first.WildcardPerfumeId, second.WildcardPerfumeId);
		// The second call must be answered from the cache entry the service stored after the first.
		ChatCompletionCanned.VerifyCompletionOnce(client);
		Assert.True(cache.TryGetValue<YearInReviewAiSelection>(CacheKey(2025, candidates), out var stored));
		Assert.NotNull(stored);
	}

	[Fact]
	public async Task SelectCategories_CachedSelection_ReturnedWithoutCallingOpenAI() {
		var candidates = BuildCandidates();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set(CacheKey(2025, candidates), new YearInReviewAiSelection(Guid.NewGuid(), "pre-cached", null, null, null, null));
		var client = ChatCompletionCanned.ClientThrowing(new InvalidOperationException("must not be called"));
		var service = BuildService(client, cache);

		var result = await service.SelectCategories(2025, candidates, TestContext.Current.CancellationToken);

		Assert.NotNull(result);
		Assert.Equal("pre-cached", result.CrowdPleaserReason);
		ChatCompletionCanned.VerifyNeverCalled(client);
	}

	[Fact]
	public async Task SelectCategories_EmptyCompletionContent_ReturnsNull() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.EmptyCompletion());
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var service = BuildService(client, cache);

		var result = await service.SelectCategories(2025, BuildCandidates(), TestContext.Current.CancellationToken);

		Assert.Null(result);
		ChatCompletionCanned.VerifyCompletionOnce(client);
	}

	[Fact]
	public async Task SelectCategories_NullJsonPayload_ReturnsNull() {
		var client = ChatCompletionCanned.ClientReturning(ChatCompletionCanned.Completion("null"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var service = BuildService(client, cache);

		var result = await service.SelectCategories(2025, BuildCandidates(), TestContext.Current.CancellationToken);

		Assert.Null(result);
	}

	[Fact]
	public async Task SelectCategories_OpenAiThrows_ReturnsNullAndLogs() {
		var client = ChatCompletionCanned.ClientThrowing(new HttpRequestException("upstream down"));
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var service = BuildService(client, cache);

		var result = await service.SelectCategories(2025, BuildCandidates(), TestContext.Current.CancellationToken);

		Assert.Null(result);
	}

	[Fact]
	public async Task SelectCategories_Cancellation_Propagates() {
		var client = ChatCompletionCanned.ClientThrowing(new OperationCanceledException());
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var service = BuildService(client, cache);

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			service.SelectCategories(2025, BuildCandidates(), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task NullYearInReviewAi_AlwaysReturnsNull() {
		var result = await new NullYearInReviewAi().SelectCategories(2025, BuildCandidates(), TestContext.Current.CancellationToken);

		Assert.Null(result);
	}
}
