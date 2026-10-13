//! A c2pa-rs signer whose private key stays in AWS KMS: each signature is one KMS
//! `Sign` call (SIGNUNDER.md §4.5).

use std::collections::HashMap;
use std::sync::{Arc, Mutex};

use aws_sdk_kms::Client;
use aws_sdk_kms::error::DisplayErrorContext;
use aws_sdk_kms::primitives::Blob;
use aws_sdk_kms::types::{MessageType, SigningAlgorithmSpec};
use c2pa::{CallbackSigner, SigningAlg};
use sha2::{Digest, Sha256};
use tokio::runtime::Handle;
use tonic::Status;
use x509_parser::pem::parse_x509_pem;

/// `SignerRef.key_ref` values with this prefix are KMS key ARNs.
pub const KEY_ARN_PREFIX: &str = "arn:aws:kms:";

/// KMS access shared by every request.
pub struct Kms {
    client: Client,
    /// Public keys (DER SubjectPublicKeyInfo) by key ARN. A KMS key's public key never
    /// changes, so entries are kept for the life of the process.
    public_keys: Mutex<HashMap<String, Vec<u8>>>,
}

impl Kms {
    /// Region, credentials and (for the local-kms emulator) `AWS_ENDPOINT_URL` come from
    /// the environment. Nothing is resolved until the first call.
    pub async fn from_env() -> Self {
        let config = aws_config::load_defaults(aws_config::BehaviorVersion::latest()).await;
        Self {
            client: Client::new(&config),
            public_keys: Mutex::default(),
        }
    }

    async fn public_key(&self, key_arn: &str) -> Result<Vec<u8>, Status> {
        if let Some(key) = self.public_keys.lock().unwrap().get(key_arn) {
            return Ok(key.clone());
        }
        let reply = self
            .client
            .get_public_key()
            .key_id(key_arn)
            .send()
            .await
            .map_err(|e| match e.as_service_error() {
                Some(e) if e.is_not_found_exception() => {
                    Status::not_found(format!("KMS key {key_arn} does not exist"))
                }
                _ => Status::unavailable(format!(
                    "KMS GetPublicKey for {key_arn}: {}",
                    DisplayErrorContext(&e)
                )),
            })?;
        let key = reply
            .public_key
            .map(Blob::into_inner)
            .ok_or_else(|| Status::internal("KMS returned no public key"))?;
        self.public_keys
            .lock()
            .unwrap()
            .insert(key_arn.to_owned(), key.clone());
        Ok(key)
    }

    /// Builds a signer for the KMS key `key_arn`, presenting `cert_chain_pem` (signing
    /// certificate first), after checking that the certificate is for that key. The
    /// signer and this call block on `runtime`, so use both from a blocking thread.
    pub fn signer(
        self: &Arc<Self>,
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

        let bad_chain = || {
            Status::invalid_argument(
                "cert_chain_pem must hold the PEM certificate chain for the KMS key",
            )
        };
        let (_, leaf) = parse_x509_pem(cert_chain_pem.as_bytes()).map_err(|_| bad_chain())?;
        let leaf = leaf.parse_x509().map_err(|_| bad_chain())?;
        if leaf.public_key().raw != runtime.block_on(self.public_key(&key_arn))? {
            return Err(Status::failed_precondition(format!(
                "the signing certificate is not for KMS key {key_arn}"
            )));
        }

        let client = self.client.clone();
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

    fn sign_with(key_arn: &str, chain: &str) -> Result<provenance::Signed, Status> {
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let kms = Arc::new(runtime.block_on(Kms::from_env()));
        let signer = kms.signer(
            runtime.handle().clone(),
            key_arn.into(),
            "es256",
            chain,
            None,
        )?;
        provenance::sign(&signer, JPEG, "image/jpeg", DEFINITION)
    }

    #[test]
    #[ignore = "needs the local-kms emulator: make test-kms"]
    fn kms_key_signs_and_validates() {
        let signed = sign_with(KEY_ARN, CHAIN).unwrap();
        assert_eq!(signed.state, ValidationState::Valid);

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
    fn unknown_kms_key_is_not_found() {
        let missing = KEY_ARN.replace("5160de01", "00000000");
        let status = sign_with(&missing, CHAIN).err().unwrap();
        assert_eq!(status.code(), tonic::Code::NotFound, "{status}");
    }

    #[test]
    #[ignore = "needs the local-kms emulator: make test-kms"]
    fn certificate_for_another_key_is_refused() {
        // The chain's second certificate is the root, whose key is not the KMS key.
        let root = &CHAIN[CHAIN.rfind("-----BEGIN CERTIFICATE-----").unwrap()..];
        let status = sign_with(KEY_ARN, root).err().unwrap();
        assert_eq!(status.code(), tonic::Code::FailedPrecondition, "{status}");
    }

    #[test]
    fn rejects_bad_requests_before_calling_kms() {
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let kms = Arc::new(runtime.block_on(Kms::from_env()));
        let build = |algorithm, chain| {
            kms.signer(
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
        assert_eq!(build("es256", "not pem"), tonic::Code::InvalidArgument);
        assert_eq!(build("rs256", CHAIN), tonic::Code::InvalidArgument);
        assert_eq!(build("ps256", CHAIN), tonic::Code::Unimplemented);
    }
}
