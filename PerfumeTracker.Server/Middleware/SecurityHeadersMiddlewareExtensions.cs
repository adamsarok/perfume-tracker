using Microsoft.AspNetCore.Builder;

namespace PerfumeTracker.Server.Middleware;

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
} 