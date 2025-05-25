namespace PerfumeTracker.Server.Features.Perfumes;

public class PerfumeValidator : AbstractValidator<PerfumeDto> {
	public PerfumeValidator() {
		RuleFor(x => x.House).Length(1, 250);
		RuleFor(x => x.PerfumeName).Length(1, 250);
		RuleFor(x => x.Ml).GreaterThan(0.1m);
		RuleFor(x => x.Rating).GreaterThanOrEqualTo(0);
	}
}
