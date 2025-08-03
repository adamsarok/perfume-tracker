using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Users;
public record CreateInviteCommand(string Email) : ICommand<CreateInviteResponse>;
public class CreateInviteCommandValidator : AbstractValidator<CreateInviteCommand> {
	public CreateInviteCommandValidator() => RuleFor(x => x.Email).EmailAddress();
}
public class CreateInviteEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/invite", async ([FromBody] CreateInviteCommand command,
		  ISender sender) => {
			  var response = await sender.Send(command);
			  return Results.Ok(response.InviteCode);
		  }).WithTags("Users")
			.WithName("CreateInvite")
			.RequireAuthorization(Roles.ADMIN);
	}
}
public record CreateInviteResponse(string Email, Guid InviteCode);
public class CreateInviteHandler(PerfumeTrackerContext context) : ICommandHandler<CreateInviteCommand, CreateInviteResponse> {
	public async Task<CreateInviteResponse> Handle(CreateInviteCommand request, CancellationToken cancellationToken) {
		var invite = new Invite() { Email = request.Email };
		context.Invites.Add(invite);
		await context.SaveChangesAsync();
		return new CreateInviteResponse(invite.Email, invite.Id);
	}
}

