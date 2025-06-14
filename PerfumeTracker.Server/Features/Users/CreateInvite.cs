namespace PerfumeTracker.Server.Features.Users;
public record CreateInviteCommand(string Email) : ICommand<CreateInviteResponse>;
public class CreateInviteEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/invite", async ([FromBody] CreateInviteCommand command,
		  ISender sender) => {
			  var response = await sender.Send(command);
			  return Results.Ok(response.InviteCode);
		  }).WithTags("Auth")
			.WithName("CreateInvite")
			.AllowAnonymous();
	}
}
public record CreateInviteResponse(string Email, Guid InviteCode);
public class CreateInviteHandler(PerfumeTrackerContext context) : ICommandHandler<CreateInviteCommand, CreateInviteResponse> {
	public async Task<CreateInviteResponse> Handle(CreateInviteCommand request, CancellationToken cancellationToken) {
		//TODO set up fluentvalidation
		if (string.IsNullOrWhiteSpace(request.Email)) throw new FieldEmptyException("Email");
		var invite = new Invite() { Email = request.Email };
		context.Invites.Add(invite);
		await context.SaveChangesAsync();
		return new CreateInviteResponse(invite.Email, invite.Id);
	}
}

