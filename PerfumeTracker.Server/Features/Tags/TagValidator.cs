namespace PerfumeTracker.Server.Features.Tags;
public class TagValidator : AbstractValidator<TagUploadDto> {
	public TagValidator() {
		RuleFor(x => x.TagName).Length(1, 250);
		RuleFor(x => x.Color).Length(7).Matches("^#[A-F0-9]{6}$").WithMessage("Not a valid hex color code");
	}
}