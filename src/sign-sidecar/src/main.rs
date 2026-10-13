use std::net::SocketAddr;
use std::path::PathBuf;
use std::sync::Arc;

use tokio::signal::unix::{SignalKind, signal};
use tonic::{Request, Response, Status, transport::Server};

mod provenance;

pub mod pb {
    tonic::include_proto!("signunder.sign.v1");
}

use pb::sign_service_server::{SignService, SignServiceServer};
use pb::{SignRequest, SignResponse, SignerRef, ValidateRequest, ValidateResponse};
use provenance::{DEV_KEY_REF, DevSigner};

/// Assets travel as unary bytes (see sign.proto), so tonic's 4 MB default is too small.
const MAX_MESSAGE_BYTES: usize = 64 * 1024 * 1024;

struct Sidecar {
    dev_signer: Option<Arc<DevSigner>>,
}

impl Sidecar {
    /// Only the development signer exists so far; KMS-backed signers are Phase 1.
    fn dev_signer(&self, signer: Option<&SignerRef>) -> Result<Arc<DevSigner>, Status> {
        let signer = signer.ok_or_else(|| Status::invalid_argument("signer is required"))?;
        if signer.key_ref != DEV_KEY_REF {
            return Err(Status::unimplemented(format!(
                "only the development signer ({DEV_KEY_REF}) is implemented"
            )));
        }
        if signer.algorithm != "es256" {
            return Err(Status::invalid_argument(
                "the development signer only signs with es256",
            ));
        }
        if !signer.cert_chain_pem.is_empty() {
            return Err(Status::invalid_argument(
                "the development signer uses its own certificate chain; leave cert_chain_pem empty",
            ));
        }
        self.dev_signer.clone().ok_or_else(|| {
            Status::failed_precondition(
                "the development signer is disabled; set SIGNUNDER_DEV_SIGNER_DIR",
            )
        })
    }
}

async fn blocking<T: Send + 'static>(
    work: impl FnOnce() -> Result<T, Status> + Send + 'static,
) -> Result<T, Status> {
    tokio::task::spawn_blocking(work)
        .await
        .map_err(|e| Status::internal(format!("worker failed: {e}")))?
}

#[tonic::async_trait]
impl SignService for Sidecar {
    async fn sign(&self, request: Request<SignRequest>) -> Result<Response<SignResponse>, Status> {
        let request = request.into_inner();
        let dev = self.dev_signer(request.signer.as_ref())?;
        let tsa_url = request
            .signer
            .map(|signer| signer.tsa_url)
            .filter(|url| !url.is_empty());

        let signed = blocking(move || {
            provenance::sign(
                &dev,
                &request.asset,
                &request.mime_type,
                &request.manifest_definition_json,
                tsa_url,
            )
        })
        .await?;

        Ok(Response::new(SignResponse {
            signed_asset: signed.asset,
            manifest_store: signed.manifest_store,
            validation_report_json: signed.report_json,
        }))
    }

    async fn validate(
        &self,
        request: Request<ValidateRequest>,
    ) -> Result<Response<ValidateResponse>, Status> {
        let request = request.into_inner();
        let (_, validation_report_json) =
            blocking(move || provenance::validate(&request.asset, &request.mime_type)).await?;
        Ok(Response::new(ValidateResponse {
            validation_report_json,
        }))
    }
}

async fn shutdown() {
    let mut term = signal(SignalKind::terminate()).expect("install SIGTERM handler");
    tokio::select! {
        _ = term.recv() => {}
        _ = tokio::signal::ctrl_c() => {}
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let addr: SocketAddr = std::env::var("SIDECAR_ADDR")
        .unwrap_or_else(|_| "0.0.0.0:50051".into())
        .parse()?;

    let dev_signer = match std::env::var_os("SIGNUNDER_DEV_SIGNER_DIR") {
        Some(dir) => {
            let dir = PathBuf::from(dir);
            let signer = DevSigner::load(&dir).map_err(|e| {
                format!("loading the development signer from {}: {e}", dir.display())
            })?;
            println!(
                "WARNING: development signer enabled from {}. Its key is public and its \
                 signatures are NON-CONFORMANT. Never enable this outside local development.",
                dir.display()
            );
            Some(Arc::new(signer))
        }
        None => None,
    };

    let (reporter, health) = tonic_health::server::health_reporter();
    reporter.set_serving::<SignServiceServer<Sidecar>>().await;

    println!(
        "sign-sidecar listening on {addr} (c2pa-rs {})",
        c2pa::VERSION
    );

    Server::builder()
        .add_service(health)
        .add_service(
            SignServiceServer::new(Sidecar { dev_signer })
                .max_decoding_message_size(MAX_MESSAGE_BYTES)
                .max_encoding_message_size(MAX_MESSAGE_BYTES),
        )
        .serve_with_shutdown(addr, shutdown())
        .await?;
    Ok(())
}
