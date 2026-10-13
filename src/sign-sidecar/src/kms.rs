//! A c2pa-rs signer whose private key stays in AWS KMS: each signature is one KMS
//! `Sign` call (SIGNUNDER.md §4.5).

use aws_sdk_kms::Client;
use aws_sdk_kms::error::DisplayErrorContext;
use aws_sdk_kms::primitives::Blob;
use aws_sdk_kms::types::{MessageType, SigningAlgorithmSpec};
use c2pa::{CallbackSigner, SigningAlg};
use sha2::{Digest, Sha256};
use tokio::runtime::Handle;
use tonic::Status;

/// `SignerRef.key_ref` values with this prefix are KMS key ARNs.
pub const KEY_ARN_PREFIX: &str = "arn:aws:kms:";

/// Region, credentials and (for the local-kms emulator) `AWS_ENDPOINT_URL` come from
/// the environment. Nothing is resolved until the first call.
pub async fn client() -> Client {
    Client::new(&aws_config::load_defaults(aws_config::BehaviorVersion::latest()).await)
}

/// Builds a signer for the KMS key `key_arn`, presenting `cert_chain_pem` (signing
/// certificate first). Its `sign` blocks on `runtime`, so use it from a blocking thread.
pub fn signer(
    client: Client,
    runtime: Handle,
    key_arn: String,
    algorithm: &str,
    cert_chain_pem: &str,
    tsa_url: Option<String>,
) -> Result<CallbackSigner, Status> {
    match algorithm {
        "es256" => {}
        "ps256" => {
            return Err(Status::unimplemented(
                "ps256 with KMS is not implemented yet",
            ));
        }
        other => {
            return Err(Status::invalid_argument(format!(
                "unknown algorithm {other:?}"
            )));
        }
    }
    if !cert_chain_pem.contains("-----BEGIN CERTIFICATE-----") {
        return Err(Status::invalid_argument(
            "cert_chain_pem must hold the PEM certificate chain for the KMS key",
        ));
    }

    let sign = move |_context: *const (), data: &[u8]| {
        // KMS takes at most 4096 bytes of raw message, so it gets the digest.
        let request = client
            .sign()
            .key_id(&key_arn)
            .signing_algorithm(SigningAlgorithmSpec::EcdsaSha256)
            .message_type(MessageType::Digest)
            .message(Blob::new(Sha256::digest(data).to_vec()))
            .send();
        let reply = runtime.block_on(request).map_err(|e| {
            let message = format!("KMS Sign with {key_arn}: {}", DisplayErrorContext(&e));
            c2pa::Error::OtherError(Box::new(std::io::Error::other(message)))
        })?;
        // DER-encoded ECDSA; c2pa-rs converts it to the fixed-width form COSE wants.
        reply
            .signature
            .map(Blob::into_inner)
            .ok_or_else(|| c2pa::Error::BadParam("KMS returned no signature".into()))
    };

    let signer = CallbackSigner::new(sign, SigningAlg::Es256, cert_chain_pem);
    Ok(match tsa_url {
        Some(url) => signer.set_tsa_url(url),
        None => signer,
    })
}

#[cfg(test)]
mod tests {
    use c2pa::ValidationState;

    use super::*;
    use crate::provenance;

    /// The key `dev-signer/local-kms-seed.yaml` gives the local-kms emulator.
    const KEY_ARN: &str =
        "arn:aws:kms:eu-central-1:111122223333:key/00000000-0000-4000-8000-00005160de01";
    const CHAIN: &str = include_str!("../dev-signer/es256.chain.pem");
    const JPEG: &[u8] = include_bytes!("../tests/fixtures/sample.jpg");
    const DEFINITION: &str = r#"{"assertions": [{"label": "c2pa.actions", "data": {"actions": [{
        "action": "c2pa.created",
        "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
    }]}}]}"#;

    fn sign_with(key_arn: &str) -> Result<provenance::Signed, Status> {
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let signer = signer(
            runtime.block_on(client()),
            runtime.handle().clone(),
            key_arn.into(),
            "es256",
            CHAIN,
            None,
        )?;
        provenance::sign(&signer, JPEG, "image/jpeg", DEFINITION)
    }

    #[test]
    #[ignore = "needs the local-kms emulator: make test-kms"]
    fn kms_key_signs_and_validates() {
        let signed = sign_with(KEY_ARN).unwrap();

        let (state, report_json) = provenance::validate(&signed.asset, "image/jpeg").unwrap();
        assert_eq!(state, ValidationState::Valid);
        let report: serde_json::Value = serde_json::from_str(&report_json).unwrap();
        let manifest = &report["manifests"][0];
        assert_eq!(manifest["signature"]["algorithm"], "es256");
        assert_eq!(
            manifest["signature"]["certificateInfo"]["subject"]["O"],
            "SignUnder DEV - NON-CONFORMANT"
        );
        let success = manifest["validationResults"]["success"].as_array().unwrap();
        assert!(
            success
                .iter()
                .any(|status| status["code"] == "claimSignature.validated")
        );
    }

    #[test]
    #[ignore = "needs the local-kms emulator: make test-kms"]
    fn unknown_kms_key_fails() {
        let missing = KEY_ARN.replace("5160de01", "00000000");
        let status = sign_with(&missing).err().unwrap();
        assert!(status.message().contains("KMS Sign"), "{status}");
    }

    #[test]
    fn rejects_bad_requests_before_calling_kms() {
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let client = runtime.block_on(client());
        let build = |algorithm, chain| {
            signer(
                client.clone(),
                runtime.handle().clone(),
                KEY_ARN.into(),
                algorithm,
                chain,
                None,
            )
            .err()
            .unwrap()
            .code()
        };
        assert_eq!(build("es256", ""), tonic::Code::InvalidArgument);
        assert_eq!(build("rs256", CHAIN), tonic::Code::InvalidArgument);
        assert_eq!(build("ps256", CHAIN), tonic::Code::Unimplemented);
    }
}
