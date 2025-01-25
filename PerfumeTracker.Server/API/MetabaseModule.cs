﻿using Carter;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using PerfumeTracker.Server.Repo;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PerfumeTracker.Server.API {
    public class MetabaseModule : ICarterModule {
        public void AddRoutes(IEndpointRouteBuilder app) {
            app.MapGet("/api/metabase", async (HttpContext context) => {
                string? url;
                string? apiKey;
                //TODO: move config out of here
                if (context.RequestServices.GetService<IWebHostEnvironment>().IsDevelopment()) {
                    var configuration = context.RequestServices.GetService<IConfiguration>();
                    url = configuration["Metabase:Url"];
                    apiKey = configuration["Metabase:SecretKey"];
                } else {
                    url = Environment.GetEnvironmentVariable("METABASE_URL");
                    apiKey = Environment.GetEnvironmentVariable("METABASE_SECRET_KEY");
                }
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey)) {
                    return Results.NotFound("Metabase config not set");
                }
                var jwtToken = getToken(2, apiKey);
                var iframeUrl = $"{url}/embed/dashboard/{jwtToken}#bordered=false&titled=false";
                return Results.Ok(new { iframeUrl });
            })
                .WithTags("Metabase")
                .WithName("GetDashboardUrl");
        }

        private string getToken(int dashboardId, string apiKey) {
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var header = new JwtHeader(credentials);
            var dash = new Dictionary<string, int>();
            dash.Add("dashboard", dashboardId);
            var pars = new Dictionary<string, string>();
            JwtPayload payload = new JwtPayload {
                                {"resource",dash } ,
                                {"params" ,pars}
                            };
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(secToken);
            return tokenString;
        }
    }
}
