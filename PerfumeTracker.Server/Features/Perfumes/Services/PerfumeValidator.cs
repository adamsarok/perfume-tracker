namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class PerfumeValidator : AbstractValidator<PerfumeUploadDto> {
	public PerfumeValidator() {
		RuleFor(x => x.House).Length(1, 250);
		RuleFor(x => x.PerfumeName).Length(1, 250);
		RuleFor(x => x.Ml).GreaterThanOrEqualTo(0);
		RuleFor(x => x.MlLeft).GreaterThanOrEqualTo(0);
	}
}
