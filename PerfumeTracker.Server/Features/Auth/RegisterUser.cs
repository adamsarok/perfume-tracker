using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Auth;
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

public class RegisterUserHandler(ICreateUser createUser) : ICommandHandler<RegisterUserCommand, Unit>  {
	public async Task<Unit> Handle(RegisterUserCommand command, CancellationToken cancellationToken) {
		await createUser.Create(command.UserName, command.Password, Roles.USER, command.Email);
		return Unit.Value;
	}
}

