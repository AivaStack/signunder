namespace Signunder.Api;

/// <summary>Names of the rate-limiting policies.</summary>
public static class RateLimits
{
    /// <summary>Per API key, or per address for callers without one.</summary>
    public const string PerKey = "per-key";
}

/// <summary>
/// Headers on every response (OWASP review, 14 Oct 2026). Signed copies under /media are
/// meant to be embedded by customers' pages on other origins; everything else is not, and
/// authenticated responses carry tenant data that must not be cached. HTTPS responses
/// tell browsers to keep to HTTPS (staging scan, 15 Oct 2026).
/// </summary>
public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        http.Response.OnStarting(() =>
        {
            var headers = http.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers["Cross-Origin-Resource-Policy"] =
                http.Request.Path.StartsWithSegments("/media") ? "cross-origin" : "same-origin";
            if (http.Request.IsHttps)
            {
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            }
            if (http.User.Identity?.IsAuthenticated == true)
            {
                headers.CacheControl = "no-store";
            }
            return Task.CompletedTask;
        });
        await next(http);
    });
}
