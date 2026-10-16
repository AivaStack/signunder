namespace Signunder.Api;

/// <summary>
/// The website (signunder.com) calls the API from the browser: the public verify page and
/// the dashboard. Only the origins in <c>Website:AllowedOrigins</c> (comma-separated) may.
/// </summary>
public static class Website
{
    public const string CorsPolicy = "website";

    public static IServiceCollection AddWebsiteCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = (config["Website:AllowedOrigins"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddCors(cors => cors.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins).WithMethods("GET", "POST").WithHeaders("Authorization", "Content-Type")));
        return services;
    }
}
