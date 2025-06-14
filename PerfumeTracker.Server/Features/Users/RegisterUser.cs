namespace PerfumeTracker.Server.Features.Users;
public record RegisterUserCommand(string UserName, string Email, string Password) : ICommand<Unit>;
public class RegisterUserEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/register", async ([FromBody] RegisterUserCommand command,
		  ISender sender) => {
			  await sender.Send(command);
			  return Results.Ok();
		  }).WithTags("Auth")
			.WithName("Register")
			.AllowAnonymous();
	}
}

public class RegisterUserHandler(ICreateUser createUser, IConfiguration configuration, PerfumeTrackerContext context) : ICommandHandler<RegisterUserCommand, Unit>  {
	public async Task<Unit> Handle(RegisterUserCommand command, CancellationToken cancellationToken) {
		var userConfig = new UserConfiguration(configuration);
		Invite? invite = null;
		if (userConfig.InviteOnlyRegistration) {
			invite = context.Invites.Where(x => x.Email == command.Email).FirstOrDefault();
			if (invite == null) throw new UnauthorizedException("Active invite code not found for this email address");
		}
		await createUser.Create(command.UserName, command.Password, Roles.USER, command.Email);
		if (invite != null) {
			invite.IsUsed = true;
			await context.SaveChangesAsync();
		}
		return Unit.Value;
	}
}

