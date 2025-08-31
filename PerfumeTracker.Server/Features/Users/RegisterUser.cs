using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Users;
public record RegisterUserCommand(string UserName, string Email, string Password, Guid InviteCode) : ICommand<Unit>;
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand> {
	public RegisterUserCommandValidator() {
		RuleFor(x => x.UserName).Length(1, 255);
		RuleFor(x => x.Email).EmailAddress().Length(1, 255);
		RuleFor(x => x.Password).Length(8, 100);
		RuleFor(x => x.InviteCode).NotEmpty().WithMessage("Invite code is required for registration");
	}
}
public class RegisterUserEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/register", async ([FromBody] RegisterUserCommand command,
		  ISender sender, CancellationToken cancellationToken) => {
			  await sender.Send(command, cancellationToken);
			  return Results.Ok();
		  }).WithTags("Users")
			.WithName("Register")
			.AllowAnonymous();
	}
}

public class RegisterUserHandler(ICreateUser createUser, IConfiguration configuration, PerfumeTrackerContext context) : ICommandHandler<RegisterUserCommand, Unit>  {
	public async Task<Unit> Handle(RegisterUserCommand command, CancellationToken cancellationToken) {
		var userConfig = new UserConfiguration(configuration);
		if (userConfig.InviteOnlyRegistration) {
			var affected = await context.Invites
				.Where(x => x.Id == command.InviteCode && x.Email == command.Email && !x.IsUsed)
				.ExecuteUpdateAsync(s => s.SetProperty(i => i.IsUsed, true), cancellationToken);
			if (affected < 1) throw new UnauthorizedException("Invite not valid or already used");
		}
		await createUser.Create(command.UserName, command.Password, Roles.USER, command.Email);
		return Unit.Value;
	}
}

