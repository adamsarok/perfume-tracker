using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Services.Auth;
using System.Diagnostics;
using System.Security.Claims;

namespace PerfumeTracker.Server.Startup;

public static partial class Startup {
	public static void SetupAuthorizations(IServiceCollection services, IConfiguration configuration) {

		services.AddIdentity<PerfumeIdentityUser, PerfumeIdentityRole>()
			.AddEntityFrameworkStores<PerfumeTrackerContext>()
			.AddDefaultTokenProviders();

		var jwtConfig = new JwtConfiguration(configuration);

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


		services.ConfigureApplicationCookie(options => {
			options.Cookie.HttpOnly = true;
			options.ExpireTimeSpan = TimeSpan.FromHours(jwtConfig.ExpirationHours);

			options.LoginPath = "/api/identity/account/login";
			options.AccessDeniedPath = "/api/identity/account/access-denied";
			options.SlidingExpiration = true;
		});

		services.AddAuthorization(options => {
			options.AddPolicy(Policies.READ, p => p.RequireRole(Roles.ADMIN, Roles.DEMO, Roles.USER));
			options.AddPolicy(Policies.WRITE, p => p.RequireRole(Roles.ADMIN, Roles.USER));
			options.AddPolicy(Policies.ADMIN, p => p.RequireRole(Roles.ADMIN));
		});

		var auth = services.AddAuthentication(options => {
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		}).AddJwtBearer(options => {
			options.TokenValidationParameters = new TokenValidationParameters {
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = jwtConfig.Issuer,
				ValidAudience = jwtConfig.Audience,
				IssuerSigningKey = jwtConfig.Key
			};
			options.Events = new JwtBearerEvents {
				OnMessageReceived = context => {
					context.Token = context.Request.Cookies["jwt"];
					return Task.CompletedTask;
				}
			};
		});

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

	private static async Task HandleOAuthCallback(TicketReceivedContext context, string provider)
	{
		var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var jwtGenerator = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenGenerator>();
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

		await jwtGenerator.WriteToken(user, context.HttpContext);

		logger.LogInformation("Successfully authenticated user: {Email} via {Provider}", email, provider);
	}
}