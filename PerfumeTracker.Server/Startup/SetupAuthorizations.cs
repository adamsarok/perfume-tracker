using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server.Services.Auth;
using System.Diagnostics;

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

		var githubClientId = configuration["Authentication:GitHub:ClientId"];
		var githubClientSecret = configuration["Authentication:GitHub:ClientSecret"];
		var githubCallbackPath = configuration["Authentication:GitHub:CallbackPath"];
		if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret) && !string.IsNullOrWhiteSpace(githubCallbackPath)) {
			auth.AddCookie("External", opts => {
				opts.Cookie.Name = "ext.auth";
				opts.Cookie.SameSite = SameSiteMode.None;
				opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
				//opts.Cookie.SecurePolicy = Env.IsDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
			}).AddGitHub("GitHub", options => {
				options.ClientId = githubClientId;
				options.ClientSecret = githubClientSecret;
				options.SignInScheme = "External";
				options.Scope.Add("user:email");
				options.CallbackPath = new PathString(githubCallbackPath);
				options.SaveTokens = true;
				options.CorrelationCookie.SameSite = SameSiteMode.None;
				options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

				options.Events = new OAuthEvents {
					OnRedirectToAuthorizationEndpoint = context => {

						// Ensure HTTPS for authorization URL in production
						var uri = new Uri(context.RedirectUri);
						if (uri.Scheme == "http") {
							var httpsUri = new UriBuilder(uri) {
								Scheme = "https",
								Port = 443
							}.Uri;
							context.RedirectUri = httpsUri.ToString();
						}
						

						context.Response.Redirect(context.RedirectUri);
						return Task.CompletedTask;
					}
				};

				//if (Env.IsDevelopment) {
				//	options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
				//} else {
				//	options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
				//}
			});
		}
	}
}