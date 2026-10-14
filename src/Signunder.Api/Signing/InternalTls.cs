using System.Net.Security;
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
        var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
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
