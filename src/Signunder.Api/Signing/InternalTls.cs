using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Signunder.Api.Signing;

/// <summary>
/// Mutual TLS from the API to the sign sidecar and the watermark worker (ADR 0010). Used
/// when a service's address is <c>https://</c>: the API presents its client certificate,
/// accepts only a server certificate issued by the internal CA for the address's host
/// name, and offers only TLS 1.3.
/// </summary>
public static class InternalTls
{
    public static SocketsHttpHandler Handler(IConfiguration config, Uri address)
    {
        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            // On Fly.io the services are suspended when idle and woken by a new connection;
            // traffic on an old one goes nowhere. So idle connections are dropped well before
            // a service could be suspended, and a dead connection is noticed within seconds
            // during a call (pings only then, so idle services can still be suspended).
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
            KeepAlivePingDelay = TimeSpan.FromSeconds(5),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
        };
        if (address.Scheme != Uri.UriSchemeHttps)
        {
            return handler;
        }

        var (ca, client) = Load(config);
        handler.SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls13,
            ClientCertificates = [client],
            RemoteCertificateValidationCallback = (_, certificate, _, errors) => Trusted(certificate, errors, ca),
        };
        return handler;
    }

    /// <summary>Fails at start-up, rather than on the first call, if an https address has no certificates.</summary>
    /// <summary>
    /// Retries a call that failed as unavailable, on a new connection, which wakes a suspended
    /// service. Watermarking and signing give the same result when repeated.
    /// </summary>
    public static ServiceConfig RetryUnavailable() => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialBackoff = TimeSpan.FromMilliseconds(500),
                    MaxBackoff = TimeSpan.FromSeconds(2),
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable },
                },
            },
        },
    };

    /// <summary>
    /// Runs a call, and runs it again (twice at most) if it failed in the transport rather
    /// than in the service: a broken HTTP/2 connection, for example to a service Fly.io has
    /// suspended, surfaces as Internal or Unavailable with a network exception underneath.
    /// The repeat opens a new connection, which wakes the service.
    /// </summary>
    public static async Task<T> WithReconnect<T>(Func<Task<T>> call)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await call();
            }
            catch (RpcException e) when (attempt < 3 && IsTransportFailure(e))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
        }
    }

    internal static bool IsTransportFailure(RpcException e) =>
        e.StatusCode is StatusCode.Internal or StatusCode.Unavailable
        && e.Status.DebugException is HttpRequestException or IOException;

    public static void EnsureConfigured(IConfiguration config, params Uri?[] addresses)
    {
        if (addresses.Any(a => a?.Scheme == Uri.UriSchemeHttps))
        {
            Load(config);
        }
    }

    private static (X509Certificate2 Ca, X509Certificate2 Client) Load(IConfiguration config)
    {
        string Path(string name) => config[$"Internal:Tls:{name}"] is { Length: > 0 } path
            ? path
            : throw new InvalidOperationException($"Internal:Tls:{name} must be set when a service address is https.");

        var ca = X509CertificateLoader.LoadCertificateFromFile(Path("CaPath"));
        using var pem = X509Certificate2.CreateFromPemFile(Path("CertPath"), Path("KeyPath"));
        // Round-trip through PKCS#12 so the private key is usable by the TLS stack on every platform.
        var client = X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);
        return (ca, client);
    }

    /// <summary>
    /// The server certificate must chain to the internal CA, and nothing else may be wrong
    /// with it; in particular, its name must match the host the API connected to.
    /// </summary>
    internal static bool Trusted(X509Certificate? certificate, SslPolicyErrors errors, X509Certificate2 ca)
    {
        if (certificate is null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None)
        {
            return false;
        }
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(ca);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        using var server = new X509Certificate2(certificate);
        return chain.Build(server);
    }
}
