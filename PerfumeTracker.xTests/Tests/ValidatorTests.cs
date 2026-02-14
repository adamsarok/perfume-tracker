using FluentValidation.TestHelper;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Tags;
using static PerfumeTracker.Server.Features.Perfumes.GetPerfumeRecommendations;
using static PerfumeTracker.Server.Features.Perfumes.GetSimilarPerfumes;

namespace PerfumeTracker.xTests.Tests;

public class ValidatorTests {

	// --- PerfumeValidator ---
	private readonly PerfumeValidator _perfumeValidator = new();

	[Fact]
	public void PerfumeValidator_ValidDto_Passes() {
		var dto = new PerfumeUploadDto("House1", "Big Citrus", "Woody", 100m, 50m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void PerfumeValidator_EmptyHouse_Fails() {
		var dto = new PerfumeUploadDto("", "Big Citrus", "Woody", 100m, 50m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.House);
	}

	[Fact]
	public void PerfumeValidator_HouseTooLong_Fails() {
		var dto = new PerfumeUploadDto(new string('A', 251), "Big Citrus", "Woody", 100m, 50m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.House);
	}

	[Fact]
	public void PerfumeValidator_EmptyPerfumeName_Fails() {
		var dto = new PerfumeUploadDto("House1", "", "Woody", 100m, 50m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.PerfumeName);
	}

	[Fact]
	public void PerfumeValidator_NegativeMl_Fails() {
		var dto = new PerfumeUploadDto("House1", "Big Citrus", "Woody", -1m, 50m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.Ml);
	}

	[Fact]
	public void PerfumeValidator_NegativeMlLeft_Fails() {
		var dto = new PerfumeUploadDto("House1", "Big Citrus", "Woody", 100m, -1m, []);
		var result = _perfumeValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.MlLeft);
	}

	// --- TagValidator ---
	private readonly TagValidator _tagValidator = new();

	[Fact]
	public void TagValidator_ValidDto_Passes() {
		var dto = new TagUploadDto("Woody", "#FF0000", "A woody scent");
		var result = _tagValidator.TestValidate(dto);
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void TagValidator_EmptyTagName_Fails() {
		var dto = new TagUploadDto("", "#FF0000", null);
		var result = _tagValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.TagName);
	}

	[Fact]
	public void TagValidator_TagNameTooLong_Fails() {
		var dto = new TagUploadDto(new string('A', 251), "#FF0000", null);
		var result = _tagValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.TagName);
	}

	[Fact]
	public void TagValidator_InvalidHexColor_Fails() {
		var dto = new TagUploadDto("Woody", "not-a-color", null);
		var result = _tagValidator.TestValidate(dto);
		result.ShouldHaveValidationErrorFor(x => x.Color);
	}

	[Fact]
	public void TagValidator_NullColor_Passes() {
		var dto = new TagUploadDto("Woody", null, null);
		var result = _tagValidator.TestValidate(dto);
		result.ShouldNotHaveValidationErrorFor(x => x.Color);
	}

	// --- GetPerfumeRecommendationsQueryValidator ---
	private readonly GetPerfumeRecommendationsQueryValidator _recValidator = new();

	[Fact]
	public void RecommendationsValidator_ValidQuery_Passes() {
		var query = new GetPerfumeRecommendationsQuery(5, null, null);
		var result = _recValidator.TestValidate(query);
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void RecommendationsValidator_CountZero_Fails() {
		var query = new GetPerfumeRecommendationsQuery(0, null, null);
		var result = _recValidator.TestValidate(query);
		result.ShouldHaveValidationErrorFor(x => x.Count);
	}

	[Fact]
	public void RecommendationsValidator_CountOver20_Fails() {
		var query = new GetPerfumeRecommendationsQuery(21, null, null);
		var result = _recValidator.TestValidate(query);
		result.ShouldHaveValidationErrorFor(x => x.Count);
	}

	[Fact]
	public void RecommendationsValidator_MoodOrOccasionStrategy_Fails() {
		var query = new GetPerfumeRecommendationsQuery(5, null, [PerfumeTracker.Server.Features.Perfumes.Services.RecommendationStrategy.MoodOrOccasion]);
		var result = _recValidator.TestValidate(query);
		result.ShouldHaveValidationErrorFor(x => x.Strategies);
	}

	// --- GetSimilarPerfumesQueryValidator ---
	private readonly GetSimilarPerfumesQueryValidator _similarValidator = new();

	[Fact]
	public void SimilarValidator_ValidQuery_Passes() {
		var query = new GetSimilarPerfumesQuery(Guid.NewGuid(), 5);
		var result = _similarValidator.TestValidate(query);
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void SimilarValidator_EmptyPerfumeId_Fails() {
		var query = new GetSimilarPerfumesQuery(Guid.Empty, 5);
		var result = _similarValidator.TestValidate(query);
		result.ShouldHaveValidationErrorFor(x => x.PerfumeId);
	}

	[Fact]
	public void SimilarValidator_CountOver20_Fails() {
		var query = new GetSimilarPerfumesQuery(Guid.NewGuid(), 21);
		var result = _similarValidator.TestValidate(query);
		result.ShouldHaveValidationErrorFor(x => x.Count);
	}
}
