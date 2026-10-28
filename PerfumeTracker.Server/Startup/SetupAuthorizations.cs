using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Users;
using System.Security.Claims;

namespace PerfumeTracker.Server.Startup;

public static partial class Startup {
	public static void SetupAuthorizations(IServiceCollection services, IConfiguration configuration) {

		services.AddIdentity<PerfumeIdentityUser, PerfumeIdentityRole>()
			.AddEntityFrameworkStores<PerfumeTrackerContext>()
			.AddDefaultTokenProviders();

		services.Configure<IdentityOptions>(options => {
			options.Password.RequireDigit = true;
			options.Password.RequireLowercase = true;
			options.Password.RequireNonAlphanumeric = false;
			options.Password.RequireUppercase = true;
			options.Password.RequiredLength = 12;
			options.Password.RequiredUniqueChars = 5;

			options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
			options.Lockout.MaxFailedAccessAttempts = 5;
			options.Lockout.AllowedForNewUsers = true;

			options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
			options.User.RequireUniqueEmail = true;
		});

		var sessionConfig = configuration.GetSection("Session").Get<SessionConfiguration>()
			?? new SessionConfiguration();

		// Identity.Application cookie; the ticket itself lives in Postgres via PostgresTicketStore.
		services.ConfigureApplicationCookie(options => {
			options.Cookie.Name = ".PerfumeTracker.Auth";
			options.Cookie.HttpOnly = true;
			options.Cookie.SameSite = SameSiteMode.None;
			options.Cookie.SecurePolicy = Env.IsDevelopment
				? CookieSecurePolicy.SameAsRequest
				: CookieSecurePolicy.Always;
			options.ExpireTimeSpan = sessionConfig.IdleTimeout;
			options.SlidingExpiration = true;
			options.Events = new CookieAuthenticationEvents {
				OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync,
				OnRedirectToLogin = context => {
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				},
				OnRedirectToAccessDenied = context => {
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				},
			};
		});

		services.AddSingleton<PostgresTicketStore>();
		// Configure<TOptions, TDep> doesn't exist, so register a named-options config resolved from DI.
		services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>>(sp =>
			new ConfigureNamedOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
				options.SessionStore = sp.GetRequiredService<PostgresTicketStore>()));

		// Identity cookies carry an encrypted session key; without a persisted key ring every
		// container replacement would invalidate all cookies. Keys live in Postgres alongside sessions.
		// Fixed application name keeps keys decryptable even if host metadata changes between deployments.
		services.AddDataProtection()
			.SetApplicationName("PerfumeTracker")
			.PersistKeysToDbContext<PerfumeTrackerContext>();

		services.AddAuthorization(options => {
			options.AddPolicy(Policies.READ, p => p.RequireRole(Roles.ADMIN, Roles.DEMO, Roles.USER));
			options.AddPolicy(Policies.WRITE, p => p.RequireRole(Roles.ADMIN, Roles.USER));
			options.AddPolicy(Policies.ADMIN, p => p.RequireRole(Roles.ADMIN));
		});

		var auth = services.AddAuthentication();

		auth.AddCookie("External", opts => {
			opts.Cookie.Name = "ext.auth";
			opts.Cookie.SameSite = SameSiteMode.None;
			opts.Cookie.SecurePolicy = Env.IsDevelopment
				? CookieSecurePolicy.SameAsRequest
				: CookieSecurePolicy.Always;
			opts.Cookie.HttpOnly = true;
		});

		var githubClientId = configuration["Authentication:GitHub:ClientId"];
		var githubClientSecret = configuration["Authentication:GitHub:ClientSecret"];
		var githubCallbackPath = configuration["Authentication:GitHub:CallbackPath"];
		if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret) && !string.IsNullOrWhiteSpace(githubCallbackPath)) {
			auth.AddGitHub("GitHub", options => {
				options.ClientId = githubClientId;
				options.ClientSecret = githubClientSecret;
				options.SignInScheme = "External";
				options.Scope.Add("user:email");
				options.CallbackPath = new PathString(githubCallbackPath);
				options.SaveTokens = true;
				options.CorrelationCookie.HttpOnly = true;
				options.CorrelationCookie.SameSite = SameSiteMode.None;
				options.CorrelationCookie.SecurePolicy = Env.IsDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

				options.Events.OnTicketReceived = async context => await HandleOAuthCallback(context, "GitHub");
			});
		}

		var googleClientId = configuration["Authentication:Google:ClientId"];
		var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
		var googleCallbackPath = configuration["Authentication:Google:CallbackPath"];
		if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret) && !string.IsNullOrWhiteSpace(googleCallbackPath)) {
			auth.AddGoogle("Google", options => {
				options.ClientId = googleClientId;
				options.ClientSecret = googleClientSecret;
				options.SignInScheme = "External";
				options.CallbackPath = new PathString(googleCallbackPath);
				options.SaveTokens = true;
				options.CorrelationCookie.HttpOnly = true;
				options.CorrelationCookie.SameSite = SameSiteMode.None;
				options.CorrelationCookie.SecurePolicy = Env.IsDevelopment
					? CookieSecurePolicy.SameAsRequest
					: CookieSecurePolicy.Always;

				options.Events.OnTicketReceived = async context => await HandleOAuthCallback(context, "Google");
			});
		}
	}

	private static async Task HandleOAuthCallback(TicketReceivedContext context, string provider) {
		var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<PerfumeIdentityUser>>();
		var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LoginGoogle>>();

		var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
		if (string.IsNullOrWhiteSpace(email)) {
			logger.LogWarning("{Provider} authentication failed: No email claim found", provider);
			context.Response.StatusCode = 401;
			context.HandleResponse();
			return;
		}

		var user = await userManager.FindByEmailAsync(email);
		if (user == null) {
			logger.LogWarning("User not found for email: {Email} from {Provider}", email, provider);
			context.Response.StatusCode = 401;
			context.HandleResponse();
			return;
		}

		await signInManager.SignInAsync(user, isPersistent: true);

		logger.LogInformation("Successfully authenticated user: {Email} via {Provider}", email, provider);
	}
}
