using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Users;

namespace PerfumeTracker.Evals.Harness;

/// <summary>
/// Creates (or reuses) the eval user and resets + seeds the deterministic wardrobe before a run.
/// Only rows belonging to the eval user are touched.
/// </summary>
public static class EvalDataSeeder {
	public const string UserEmail = "chat-agent-eval@example.com";
	public const string UserName = "chat-agent-eval";
	public const string UserPassword = "Eval1234!ABCDxyz59697";

	public static async Task<Guid> EnsureUserAsync(IServiceProvider services, CancellationToken cancellationToken) {
		var userManager = services.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var existing = await userManager.FindByEmailAsync(UserEmail);
		if (existing != null) return existing.Id;

		var createUser = services.GetRequiredService<ICreateUser>();
		var created = await createUser.Create(UserName, UserPassword, Roles.USER, UserEmail, isEmailConfirmed: true)
			?? throw new InvalidOperationException($"Could not create eval user {UserEmail}.");
		return created.Id;
	}

	public static async Task ResetAndSeedAsync(PerfumeTrackerContext context, Guid userId, CancellationToken cancellationToken) {
		await ResetAsync(context, userId, cancellationToken);
		await SeedAsync(context, userId, cancellationToken);
	}

	private static async Task ResetAsync(PerfumeTrackerContext context, Guid userId, CancellationToken cancellationToken) {
		await context.ChatMessages.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.ChatConversations.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.PerfumeRecommendations.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.OutboxMessages.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.PerfumeEvents.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.PerfumeRatings.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.PerfumeTags.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.Tags.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.Perfumes.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
		await context.MarketplaceOffers.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
	}

	private static async Task SeedAsync(PerfumeTrackerContext context, Guid userId, CancellationToken cancellationToken) {
		var now = DateTime.UtcNow;

		var tags = EvalWardrobe.TagNames.ToDictionary(
			name => name,
			name => new Tag {
				Id = EvalWardrobe.Id("tag", name),
				TagName = name,
				NoteGroup = EvalWardrobe.TagNoteGroups[name],
				UserId = userId,
				CreatedAt = now,
				UpdatedAt = now
			},
			StringComparer.OrdinalIgnoreCase);
		context.Tags.AddRange(tags.Values);

		var perfumes = new List<Perfume>();
		var perfumeTags = new List<PerfumeTag>();
		var ratings = new List<PerfumeRating>();

		foreach (var seed in EvalWardrobe.Perfumes) {
			perfumes.Add(new Perfume {
				Id = seed.Id,
				House = seed.House,
				PerfumeName = seed.PerfumeName,
				Family = seed.Family,
				Parfumeur = seed.Parfumeur,
				Ml = seed.Ml,
				MlLeft = seed.MlLeft,
				LatestRating = seed.Rating,
				WearCount = seed.WearCount,
				LastWorn = seed.DaysSinceLastWorn.HasValue ? now.AddDays(-seed.DaysSinceLastWorn.Value) : null,
				UserId = userId,
				CreatedAt = now,
				UpdatedAt = now
			});

			foreach (var tagName in seed.Tags) {
				if (!tags.TryGetValue(tagName, out var tag))
					throw new InvalidOperationException($"Wardrobe perfume '{seed.Key}' references unknown tag '{tagName}'.");
				perfumeTags.Add(new PerfumeTag {
					Id = EvalWardrobe.Id("perfume-tag", $"{seed.Key}:{tagName}"),
					PerfumeId = seed.Id,
					TagId = tag.Id,
					UserId = userId,
					CreatedAt = now,
					UpdatedAt = now
				});
			}

			if (seed.Rating > 0 || !string.IsNullOrWhiteSpace(seed.Comment)) {
				ratings.Add(new PerfumeRating {
					Id = EvalWardrobe.Id("rating", seed.Key),
					PerfumeId = seed.Id,
					Rating = seed.Rating,
					Comment = seed.Comment ?? string.Empty,
					RatingDate = now.AddDays(-(seed.DaysSinceLastWorn ?? 1)),
					UserId = userId,
					CreatedAt = now,
					UpdatedAt = now
				});
			}
		}

		context.Perfumes.AddRange(perfumes);
		context.PerfumeTags.AddRange(perfumeTags);
		context.PerfumeRatings.AddRange(ratings);

		context.MarketplaceOffers.AddRange(EvalWardrobe.Offers.Select(offer => new MarketplaceOffer {
			Id = EvalWardrobe.Id("offer", $"{offer.Source}:{offer.SourceOfferId}"),
			Source = offer.Source,
			SourceOfferId = offer.SourceOfferId,
			SourceUrl = $"https://example.invalid/{offer.Source.ToLowerInvariant()}/{offer.SourceOfferId}",
			SellerName = offer.SellerName,
			BrandName = offer.BrandName,
			ProductName = offer.ProductName,
			OfferTitle = offer.OfferTitle,
			PriceText = offer.PriceText,
			SizeText = offer.SizeText,
			Status = offer.Status,
			ListedAt = now.AddDays(-offer.DaysSinceListed),
			ImportedAt = now.AddDays(-offer.DaysSinceListed),
			UserId = userId,
			CreatedAt = now,
			UpdatedAt = now
		}));

		await context.SaveChangesAsync(cancellationToken);
	}
}
