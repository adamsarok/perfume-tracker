using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

		services.AddAuthentication(options => {
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
	}
}