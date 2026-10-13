use std::net::SocketAddr;
use std::path::PathBuf;
use std::sync::Arc;

use tokio::signal::unix::{SignalKind, signal};
use tonic::{Request, Response, Status, transport::Server};

mod kms;
mod provenance;

pub mod pb {
    tonic::include_proto!("signunder.sign.v1");
}

use kms::KEY_ARN_PREFIX;
use pb::sign_service_server::{SignService, SignServiceServer};
use pb::{SignRequest, SignResponse, SignerRef, ValidateRequest, ValidateResponse};
use provenance::{DEV_KEY_REF, DevSigner};

/// Assets travel as unary bytes (see sign.proto), so tonic's 4 MB default is too small.
const MAX_MESSAGE_BYTES: usize = 64 * 1024 * 1024;

struct Sidecar {
    dev_signer: Option<Arc<DevSigner>>,
    kms: aws_sdk_kms::Client,
}

impl Sidecar {
    /// Checks a development-signer request and returns the signer's key material.
    fn dev_signer(&self, signer: &SignerRef) -> Result<Arc<DevSigner>, Status> {
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
        let signer = request
            .signer
            .ok_or_else(|| Status::invalid_argument("signer is required"))?;
        let tsa_url = Some(signer.tsa_url.clone()).filter(|url| !url.is_empty());

        let dev = if signer.key_ref == DEV_KEY_REF {
            Some(self.dev_signer(&signer)?)
        } else if signer.key_ref.starts_with(KEY_ARN_PREFIX) {
            None
        } else {
            return Err(Status::invalid_argument(format!(
                "key_ref must be a KMS key ARN or {DEV_KEY_REF}"
            )));
        };
        let kms = self.kms.clone();
        let runtime = tokio::runtime::Handle::current();

        let signed = blocking(move || {
            let signer: Box<dyn c2pa::Signer> = match dev {
                Some(dev) => dev.signer(tsa_url)?,
                None => Box::new(kms::signer(
                    kms,
                    runtime,
                    signer.key_ref,
                    &signer.algorithm,
                    &signer.cert_chain_pem,
                    tsa_url,
                )?),
            };
            provenance::sign(
                signer.as_ref(),
                &request.asset,
                &request.mime_type,
                &request.manifest_definition_json,
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
            SignServiceServer::new(Sidecar {
                dev_signer,
                kms: kms::client().await,
            })
            .max_decoding_message_size(MAX_MESSAGE_BYTES)
            .max_encoding_message_size(MAX_MESSAGE_BYTES),
        )
        .serve_with_shutdown(addr, shutdown())
        .await?;
    Ok(())
}
