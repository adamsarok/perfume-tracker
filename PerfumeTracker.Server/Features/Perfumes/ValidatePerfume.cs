namespace PerfumeTracker.Server.Features.Perfumes;
public class PerfumeUploadValidator : AbstractValidator<PerfumeUploadDto> {
	public PerfumeUploadValidator() {
		RuleFor(x => x.House).Length(1, 250);
		RuleFor(x => x.PerfumeName).Length(1, 250);
		RuleFor(x => x.Ml).GreaterThan(0.1m);
	}
}
