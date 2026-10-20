using Microsoft.Extensions.DependencyInjection;

namespace Signunder.Sdk;

/// <summary>Settings for <see cref="SignunderServiceCollectionExtensions.AddSignunderClient"/>.</summary>
public sealed class SignunderClientOptions
{
    /// <summary>The API's address; <see cref="SignunderClient.DefaultBaseAddress"/> if not set.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>A SignUnder API key (<c>su_live_…</c> or <c>su_test_…</c>).</summary>
    public string? ApiKey { get; set; }
}

/// <summary>Registers <see cref="SignunderClient"/> with dependency injection.</summary>
public static class SignunderServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="SignunderClient"/> on a pooled <see cref="HttpClient"/>. The returned
    /// builder takes the usual handlers, for example for retries.
    /// </summary>
    public static IHttpClientBuilder AddSignunderClient(this IServiceCollection services, Action<SignunderClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new SignunderClientOptions();
        configure(options);
        return services.AddHttpClient<SignunderClient>(http => SignunderClient.Configure(http, options.BaseAddress, options.ApiKey));
    }
}
