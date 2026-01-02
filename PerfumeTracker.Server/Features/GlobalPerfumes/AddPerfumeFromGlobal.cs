using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.GlobalPerfumes;

public record AddPerfumeFromGlobalCommand(Guid GlobalPerfumeId, decimal Ml, decimal MlLeft) : ICommand<PerfumeDto>;

public class AddPerfumeFromGlobalEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes/from-global/{globalPerfumeId}",
			async (Guid globalPerfumeId, AddPerfumeFromGlobalRequest request, ISender sender, CancellationToken cancellationToken) => {
				var result = await sender.Send(
					new AddPerfumeFromGlobalCommand(globalPerfumeId, request.Ml, request.MlLeft),
					cancellationToken);
				return Results.CreatedAtRoute("GetPerfume", new { id = result.Id }, result);
			})
			.WithTags("Perfumes")
			.WithName("AddPerfumeFromGlobal")
			.RequireAuthorization(Policies.WRITE);
	}
}

public record AddPerfumeFromGlobalRequest(decimal Ml, decimal MlLeft);

public class AddPerfumeFromGlobalHandler(PerfumeTrackerContext context, ISideEffectQueue queue)
	: ICommandHandler<AddPerfumeFromGlobalCommand, PerfumeDto> {

	public async Task<PerfumeDto> Handle(AddPerfumeFromGlobalCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

		// Get the global perfume with its tags
		var globalPerfume = await context.GlobalPerfumes
			.Include(x => x.GlobalPerfumeTags)
			.ThenInclude(x => x.GlobalTag)
			.FirstOrDefaultAsync(x => x.Id == request.GlobalPerfumeId, cancellationToken)
			?? throw new NotFoundException($"GlobalPerfume with id {request.GlobalPerfumeId} not found");

		// Check if user already has this perfume
		var existingPerfume = await context.Perfumes
			.FirstOrDefaultAsync(p =>
				p.House == globalPerfume.House &&
				p.PerfumeName == globalPerfume.PerfumeName,
				cancellationToken);

		if (existingPerfume != null) {
			throw new InvalidOperationException($"You already have this perfume: {existingPerfume.House} - {existingPerfume.PerfumeName}");
		}

		// Create new user perfume from global perfume
		var perfume = new Perfume {
			House = globalPerfume.House,
			PerfumeName = globalPerfume.PerfumeName,
			Family = globalPerfume.Family,
			Ml = request.Ml,
			MlLeft = request.MlLeft,
			ImageObjectKeyNew = globalPerfume.ImageObjectKeyNew,
		};

		context.Perfumes.Add(perfume);

		// Get or create corresponding user tags from global tags
		foreach (var globalPerfumeTag in globalPerfume.GlobalPerfumeTags) {
			var userTag = await context.Tags
				.FirstOrDefaultAsync(t => t.TagName == globalPerfumeTag.GlobalTag.TagName, cancellationToken);

			if (userTag == null) {
				// Create user tag from global tag
				userTag = new Tag {
					TagName = globalPerfumeTag.GlobalTag.TagName,
					Color = globalPerfumeTag.GlobalTag.Color
				};
				context.Tags.Add(userTag);
				await context.SaveChangesAsync(cancellationToken); // Save to get the Id
			}

			context.PerfumeTags.Add(new PerfumeTag {
				PerfumeId = perfume.Id,
				TagId = userTag.Id
			});
		}

		// Add initial amount event if there's liquid
		if (request.MlLeft > 0) {
			context.PerfumeEvents.Add(new PerfumeEvent {
				AmountMl = request.MlLeft,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				Perfume = perfume,
				Type = PerfumeEvent.PerfumeEventType.Added,
				UpdatedAt = DateTime.UtcNow
			});
		}

		var message = OutboxMessage.From(new PerfumeAddedNotification(perfume.Id, userId));
		context.OutboxMessages.Add(message);

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
		queue.Enqueue(message);

		return perfume.Adapt<PerfumeDto>();
	}
}