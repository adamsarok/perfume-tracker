using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Auth;
public record RegisterUserCommand(string UserName, string Email, string Password) : ICommand<RegisterUserResult>;
public class RegisterUserEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/register", async ([FromBody] RegisterUserCommand command,
		  ISender sender) => {
			  var response = await sender.Send(command);
			  return Results.Ok(response);
		  }).WithTags("Auth")
			.WithName("Register")
			.AllowAnonymous();
	}
}

public record RegisterUserResult(string UserName, string Email, Guid UserId);
public class RegisterUserHandler(UserManager<PerfumeIdentityUser> userManager, PerfumeTrackerContext context) : ICommandHandler<RegisterUserCommand, RegisterUserResult>  {
	public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken) {
		var user = await userManager.FindByEmailAsync(command.Email);
		if (user == null) {
			user = new PerfumeIdentityUser {
				UserName = command.UserName,
				Email = command.Email,
				EmailConfirmed = false
			};
			var result = await userManager.CreateAsync(user, command.Password);
			if (!result.Succeeded) {
				throw new InvalidOperationException("Failed to create user: " + string.Join(", ", result.Errors.Select(x => x.Description)));
			}
			await userManager.AddToRoleAsync(user, Roles.ADMIN);
			context.UserProfiles.Add(new UserProfile(user.Id, command.UserName, command.Email));
			await context.SaveChangesAsync();
			return new RegisterUserResult(command.UserName, command.Email, user.Id);
		} else {
			throw new InvalidOperationException("Email already exists"); //TODO prevent email renumeration by always returning ok in live version
		}
	}
}

