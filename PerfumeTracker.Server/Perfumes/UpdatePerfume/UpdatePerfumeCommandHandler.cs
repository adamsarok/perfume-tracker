using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.CQRS;
using PerfumeTracker.Server.DTO;
using PerfumeTracker.Server.Models;
using static PerfumeTracker.Server.Repo.PerfumeRepo;
using static PerfumeTracker.Server.Repo.ResultType;

namespace PerfumeTracker.Server.Perfumes.UpdatePerfume {
	public record UpdatePerfumeCommand(PerfumeDTO Perfume) : ICommand<UpdatePerfumeResult>;
	public record UpdatePerfumeResult(bool IsSuccess);
	public class UpdatePerfumeCommandValidator : AbstractValidator<UpdatePerfumeCommand> {
		public UpdatePerfumeCommandValidator() {
			RuleFor(command => command.Perfume).NotNull(); //TODO messages
			RuleFor(command => command.Perfume.PerfumeName).NotEmpty()
				.Length(2, 255);
			RuleFor(command => command.Perfume.House).NotEmpty()
				.Length(2, 255);
			RuleFor(command => command.Perfume.Rating).NotEmpty()
				.InclusiveBetween(0, 10);
			//todo further rules
		}
	}
	public class PerfumeNotFoundException : Exception {
		public PerfumeNotFoundException(int id)
			: base($"Perfume {id} not found") { }
	}
	public class PerfumeAdaptException() : Exception();
	public class UpdatePerfumeCommandHandler(PerfumetrackerContext context)
		: ICommandHandler<UpdatePerfumeCommand, UpdatePerfumeResult> {
		public async Task<UpdatePerfumeResult> Handle(UpdatePerfumeCommand request, CancellationToken cancellationToken) {
			var perfume = request.Perfume.Adapt<Models.Perfume>();
			if (perfume == null) throw new PerfumeAdaptException();

			var find = await context
				.Perfumes
				.Include(x => x.PerfumeTags)
				.ThenInclude(x => x.Tag)
				.FirstOrDefaultAsync(x => x.Id == perfume.Id);
			if (find == null) throw new PerfumeNotFoundException(perfume.Id);

			context.Entry(find).CurrentValues.SetValues(perfume);
			find.Updated_At = DateTime.UtcNow;

			await UpdateTags(request.Perfume, find);

			return new UpdatePerfumeResult(true);

		}

		//this is already... interesting. there must be shared code between update & create
		//maybe all perfume commands should be the slice?

		private async Task UpdateTags(PerfumeDTO dto, Perfume? find) {
			var tagsInDB = find.PerfumeTags
				.Select(x => x.Tag)
				.Select(x => x.TagName)
				.ToList();
			foreach (var remove in find.PerfumeTags.Where(x => !dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
				context.PerfumeTags.Remove(remove);
			}
			foreach (var add in dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				context.PerfumeTags.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
				});
			}
			await context.SaveChangesAsync();
		}
	}
}

