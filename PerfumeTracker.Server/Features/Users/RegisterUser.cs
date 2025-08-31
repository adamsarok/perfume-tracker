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
		Invite? invite = null;
		if (userConfig.InviteOnlyRegistration) {
			using var transaction = await context.Database.BeginTransactionAsync();
			try {
				invite = await context.Invites
					.Where(x => x.Email == command.Email && x.Id == command.InviteCode)
					.FirstOrDefaultAsync(cancellationToken);
				if (invite == null) throw new UnauthorizedException("Active invite code not found for this email address");
				await createUser.Create(command.UserName, command.Password, Roles.USER, command.Email);
				invite.IsUsed = true;
				await context.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);
				return Unit.Value;
			} catch {
				await transaction.RollbackAsync(cancellationToken);
				throw;
			}
		}
		await createUser.Create(command.UserName, command.Password, Roles.USER, command.Email);
		return Unit.Value;
	}
}

