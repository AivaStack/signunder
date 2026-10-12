use std::net::SocketAddr;

use tokio::signal::unix::{SignalKind, signal};
use tonic::{Request, Response, Status, transport::Server};

pub mod pb {
    tonic::include_proto!("signunder.sign.v1");
}

use pb::sign_service_server::{SignService, SignServiceServer};
use pb::{SignRequest, SignResponse, ValidateRequest, ValidateResponse};

#[derive(Default)]
struct Sidecar;

#[tonic::async_trait]
impl SignService for Sidecar {
    async fn sign(&self, _request: Request<SignRequest>) -> Result<Response<SignResponse>, Status> {
        Err(Status::unimplemented("sign is not implemented yet"))
    }

    async fn validate(
        &self,
        _request: Request<ValidateRequest>,
    ) -> Result<Response<ValidateResponse>, Status> {
        Err(Status::unimplemented("validate is not implemented yet"))
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

    let (reporter, health) = tonic_health::server::health_reporter();
    reporter.set_serving::<SignServiceServer<Sidecar>>().await;

    println!(
        "sign-sidecar listening on {addr} (c2pa-rs {})",
        c2pa::VERSION
    );

    Server::builder()
        .add_service(health)
        .add_service(SignServiceServer::new(Sidecar))
        .serve_with_shutdown(addr, shutdown())
        .await?;
    Ok(())
}
