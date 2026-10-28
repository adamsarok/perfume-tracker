using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.xTests.Tests;

public class PerfumeNameMatcherTests {
	[Theory]
	[InlineData("Chanel EDP", "chanel eau de parfum")]
	[InlineData(" Dior EDT sample", "dior eau de toilette sample")]
	[InlineData("EDC Acqua", "eau de cologne acqua")]
	[InlineData("edp EDT edc", "eau de parfum eau de toilette eau de cologne")]
	[InlineData("no abbreviations here", "no abbreviations here")]
	public void NormalizePerfumeName_ExpandsAbbreviations(string input, string expected) {
		Assert.Equal(expected, PerfumeNameMatcher.NormalizePerfumeName(input));
	}

	[Theory]
	[InlineData("chanel", "chanel", true)]
	[InlineData("chanel", "chanel beauty", true)]
	[InlineData("tom ford", "ford", true)]
	[InlineData("ford", "tom ford", true)]
	[InlineData("le labo", "labo fragrances", false)]
	[InlineData("chanel", "dior", false)]
	[InlineData("creed", "acqua di parma", false)]
	public void HousesMatch_CoversEqualityContainmentAndSingleWord(string house1, string house2, bool expected) {
		Assert.Equal(expected, PerfumeNameMatcher.HousesMatch(house1, house2));
	}

	[Theory]
	[InlineData("bleu de chanel", "chanel", "bleu de chanel")]
	[InlineData("chanel bleu", "chanel", "bleu")]
	[InlineData("tom ford ford shadow", "tom ford", "ford shadow")]
	[InlineData("", "chanel", "")]
	[InlineData("   ", "   ", "   ")]
	[InlineData("ford tom", "tom ford", "ford tom")]
	[InlineData("dior sauvage", "givenchy", "dior sauvage")]
	public void RemoveHousePrefix_StripsLeadingHouseWords(string perfumeName, string house, string expected) {
		Assert.Equal(expected, PerfumeNameMatcher.RemoveHousePrefix(perfumeName, house));
	}

	[Fact]
	public void CalculateSimilarity_ReturnsZeroWhenHousesDiffer() {
		Assert.Equal(0, PerfumeNameMatcher.CalculateSimilarity("chanel", "bleu", "dior", "bleu"));
	}

	[Fact]
	public void CalculateSimilarity_ReturnsOneForIdenticalHouseAndName() {
		Assert.Equal(1, PerfumeNameMatcher.CalculateSimilarity("Chanel", "Bleu De Chanel", "chanel", "bleu de chanel"));
	}

	[Fact]
	public void CalculateSimilarity_StripsHousePrefixFromNamesBeforeComparing() {
		Assert.Equal(1, PerfumeNameMatcher.CalculateSimilarity("chanel", "chanel bleu", "chanel", "bleu"));
	}

	[Fact]
	public void CalculateSimilarity_UsesCoverageScoreForPartialNameOverlap() {
		var score = PerfumeNameMatcher.CalculateSimilarity("dior", "sauvage eau de", "dior", "sauvage eau");
		Assert.Equal(2d / 3d, score, 10);
	}

	[Fact]
	public void CalculateSimilarity_ReturnsZeroWhenTargetNameIsBlank() {
		Assert.Equal(0, PerfumeNameMatcher.CalculateSimilarity("dior", "sauvage", "dior", ""));
	}

	[Fact]
	public void CalculateSimilarity_ExpandsConcentrationAbbreviationsBeforeMatching() {
		Assert.Equal(1, PerfumeNameMatcher.CalculateSimilarity("chanel", "bleu EDP", "chanel", "bleu eau de parfum"));
	}
}
