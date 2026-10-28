using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.Features.Users;

public class LoginEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/login", async ([FromBody] LoginRequest request,
		  ISender sender, CancellationToken cancellationToken) => {
			  var result = await sender.Send(new LoginUserCommand(request.Email.Trim(), request.Password), cancellationToken);
			  return result.Result;
		  }).WithTags("Users")
			.WithName("LoginUser")
			.AllowAnonymous();
		app.MapPost("/api/identity/account/login/demo", async (ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new LoginDemoUserCommand(), cancellationToken);
		}).WithTags("Users")
			.WithName("LoginDemoUser")
			.AllowAnonymous();
	}
}

public record LoginUserCommand(string EmailOrUserName, string Password) : ICommand<LoginResult>;
public record LoginDemoUserCommand() : ICommand<LoginResult>;
public record LoginResult(IResult Result);
public class LoginUserHandler(UserManager<PerfumeIdentityUser> userManager, SignInManager<PerfumeIdentityUser> signInManager)
	: ICommandHandler<LoginUserCommand, LoginResult> {
	public async Task<LoginResult> Handle(LoginUserCommand command, CancellationToken cancellationToken) {
		var user = await userManager.FindByEmailAsync(command.EmailOrUserName);
		if (user == null) user = await userManager.FindByNameAsync(command.EmailOrUserName);
		if (user == null) return new LoginResult(Results.Unauthorized());
		var signInResult = await signInManager.PasswordSignInAsync(user, command.Password, isPersistent: true, lockoutOnFailure: true);
		if (!signInResult.Succeeded) return new LoginResult(Results.Unauthorized());
		return new LoginResult(Results.Ok(new { message = "Logged in successfully" }));
	}
}

public class LoginDemoUserHandler(UserManager<PerfumeIdentityUser> userManager, SignInManager<PerfumeIdentityUser> signInManager
	, IConfiguration configuration, ILogger<LoginDemoUserHandler> logger) : ICommandHandler<LoginDemoUserCommand, LoginResult> {
	public async Task<LoginResult> Handle(LoginDemoUserCommand command, CancellationToken cancellationToken) {
		var email = configuration["Users:DemoEmail"];
		if (string.IsNullOrWhiteSpace(email)) return new LoginResult(Results.NotFound("Demo user not configured"));
		var user = await userManager.FindByEmailAsync(email);
		if (user == null) return new LoginResult(Results.NotFound("Demo user not configured"));
		var roles = await userManager.GetRolesAsync(user);
		if (roles.Count != 1 || !roles.Contains(Roles.DEMO)) {
			logger.Log(LogLevel.Error, "Demo user has incorrect role setup: {Roles}", string.Join(", ", roles));
			return new LoginResult(Results.Unauthorized());
		}
		await signInManager.SignInAsync(user, isPersistent: true);
		logger.Log(LogLevel.Warning, "Demo user logged in");
		return new LoginResult(Results.Ok(new { message = "Logged in successfully" }));
	}
}
