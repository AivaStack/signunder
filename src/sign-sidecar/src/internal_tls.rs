//! Mutual TLS between the API and the sidecar (ADR 0010). The sidecar presents its own
//! certificate, requires one from the internal CA, and accepts only the API's.

use std::sync::Arc;

use tonic::service::Interceptor;
use tonic::transport::{Certificate, Identity, ServerTlsConfig};
use tonic::{Request, Status};
use x509_parser::extensions::GeneralName;
use x509_parser::prelude::{FromDer, X509Certificate};

/// Picks rustls's cryptography provider for the process. Both ring (tonic) and aws-lc
/// (the AWS SDK's HTTPS client) are compiled in, and rustls will not choose between them.
pub fn install_crypto_provider() {
    // An error means a provider is already installed, which is all this needs.
    let _ = rustls::crypto::ring::default_provider().install_default();
}

/// The name in the API's client certificate.
pub const API_CLIENT_NAME: &str = "signunder-api";

/// Server TLS from `SIDECAR_TLS_CERT`, `SIDECAR_TLS_KEY` and `SIDECAR_TLS_CLIENT_CA`
/// (PEM files). `None` when none is set; an error when only some are.
pub fn from_env() -> Result<Option<ServerTlsConfig>, String> {
    let paths = [
        "SIDECAR_TLS_CERT",
        "SIDECAR_TLS_KEY",
        "SIDECAR_TLS_CLIENT_CA",
    ]
    .map(|name| std::env::var_os(name).map(|path| (name, path)));
    match paths {
        [None, None, None] => Ok(None),
        [Some(cert), Some(key), Some(ca)] => {
            let read = |(name, path): (&str, std::ffi::OsString)| {
                std::fs::read(&path)
                    .map_err(|e| format!("{name} ({}): {e}", path.to_string_lossy()))
            };
            Ok(Some(
                ServerTlsConfig::new()
                    .identity(Identity::from_pem(read(cert)?, read(key)?))
                    .client_ca_root(Certificate::from_pem(read(ca)?)),
            ))
        }
        _ => Err(
            "set all of SIDECAR_TLS_CERT, SIDECAR_TLS_KEY and SIDECAR_TLS_CLIENT_CA, or none"
                .into(),
        ),
    }
}

/// Whether a DER certificate names `expected` as a DNS subject alternative name.
pub fn names(der: &[u8], expected: &str) -> bool {
    let Ok((_, cert)) = X509Certificate::from_der(der) else {
        return false;
    };
    let Ok(Some(san)) = cert.subject_alternative_name() else {
        return false;
    };
    san.value
        .general_names
        .iter()
        .any(|name| matches!(name, GeneralName::DNSName(dns) if *dns == expected))
}

/// Admits only calls whose client certificate names `client`. With TLS off it admits
/// everything: the transport then has no client certificates to check.
#[derive(Clone)]
pub struct RequireClient {
    client: Option<Arc<str>>,
}

impl RequireClient {
    pub fn new(tls: bool) -> Self {
        Self {
            client: tls.then(|| Arc::from(API_CLIENT_NAME)),
        }
    }
}

impl Interceptor for RequireClient {
    fn call(&mut self, request: Request<()>) -> Result<Request<()>, Status> {
        let Some(client) = &self.client else {
            return Ok(request);
        };
        let certs = request
            .peer_certs()
            .ok_or_else(|| Status::unauthenticated("a client certificate is required"))?;
        match certs.first() {
            Some(leaf) if names(leaf, client) => Ok(request),
            _ => Err(Status::permission_denied(
                "this client certificate may not call the sidecar",
            )),
        }
    }
}

#[cfg(test)]
mod tests {
    use std::path::PathBuf;

    use tokio_stream::wrappers::TcpListenerStream;
    use tonic::service::interceptor::InterceptedService;
    use tonic::transport::{Channel, ClientTlsConfig, Server};
    use tonic_health::pb::HealthCheckRequest;
    use tonic_health::pb::health_client::HealthClient;

    use super::*;

    fn dev(file: &str) -> Vec<u8> {
        let dir = PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("../../deploy/internal-tls/dev");
        std::fs::read(dir.join(file)).unwrap()
    }

    fn der(file: &str) -> Vec<u8> {
        pem::parse(dev(file)).unwrap().contents().to_vec()
    }

    #[test]
    fn only_the_api_certificate_names_the_api() {
        assert!(names(&der("signunder-api.pem"), API_CLIENT_NAME));
        assert!(!names(&der("sign-sidecar.pem"), API_CLIENT_NAME));
        assert!(!names(&der("wm-worker.pem"), API_CLIENT_NAME));
        assert!(!names(&der("other-client.pem"), API_CLIENT_NAME));
        assert!(!names(b"not a certificate", API_CLIENT_NAME));
    }

    /// Starts a health service behind mutual TLS and the client check; returns its address.
    async fn serve() -> std::net::SocketAddr {
        install_crypto_provider();
        let listener = tokio::net::TcpListener::bind("127.0.0.1:0").await.unwrap();
        let addr = listener.local_addr().unwrap();
        let tls = ServerTlsConfig::new()
            .identity(Identity::from_pem(
                dev("sign-sidecar.pem"),
                dev("sign-sidecar.key"),
            ))
            .client_ca_root(Certificate::from_pem(dev("ca.pem")));
        let (reporter, health) = tonic_health::server::health_reporter();
        reporter
            .set_service_status("", tonic_health::ServingStatus::Serving)
            .await;
        tokio::spawn(async move {
            Server::builder()
                .tls_config(tls)
                .unwrap()
                .add_service(InterceptedService::new(health, RequireClient::new(true)))
                .serve_with_incoming(TcpListenerStream::new(listener))
                .await
                .unwrap();
        });
        addr
    }

    async fn check(
        addr: std::net::SocketAddr,
        identity: Option<(&str, &str)>,
    ) -> Result<(), String> {
        let mut tls = ClientTlsConfig::new()
            .ca_certificate(Certificate::from_pem(dev("ca.pem")))
            .domain_name("sign-sidecar");
        if let Some((cert, key)) = identity {
            tls = tls.identity(Identity::from_pem(dev(cert), dev(key)));
        }
        let channel = Channel::from_shared(format!("https://{addr}"))
            .unwrap()
            .tls_config(tls)
            .map_err(|e| e.to_string())?
            .connect()
            .await
            .map_err(|e| format!("connect: {e}"))?;
        HealthClient::new(channel)
            .check(HealthCheckRequest {
                service: String::new(),
            })
            .await
            .map(|_| ())
            .map_err(|status| format!("{:?}: {}", status.code(), status.message()))
    }

    #[tokio::test]
    async fn the_api_certificate_is_accepted() {
        let addr = serve().await;
        check(addr, Some(("signunder-api.pem", "signunder-api.key")))
            .await
            .unwrap();
    }

    #[tokio::test]
    async fn a_call_without_a_client_certificate_is_refused() {
        let addr = serve().await;
        // The same server accepts the API (so it is up), and refuses this caller.
        check(addr, Some(("signunder-api.pem", "signunder-api.key")))
            .await
            .unwrap();
        assert!(check(addr, None).await.is_err());
    }

    #[tokio::test]
    async fn a_client_certificate_for_someone_else_is_refused_by_name() {
        // A valid client certificate from the same CA that does not name the API: the
        // handshake succeeds, so this is the interceptor's name check refusing it.
        let addr = serve().await;
        let refused = check(addr, Some(("other-client.pem", "other-client.key")))
            .await
            .unwrap_err();
        assert!(refused.starts_with("PermissionDenied"), "{refused}");
    }

    #[tokio::test]
    async fn another_service_certificate_is_refused() {
        // The worker's certificate is from the same CA but is a server certificate.
        let addr = serve().await;
        check(addr, Some(("signunder-api.pem", "signunder-api.key")))
            .await
            .unwrap();
        assert!(
            check(addr, Some(("wm-worker.pem", "wm-worker.key")))
                .await
                .is_err()
        );
    }
}
