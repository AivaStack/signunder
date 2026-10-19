//! A c2pa-rs signer whose private key stays in a cloud KMS: each signature is one KMS
//! signing call on a digest (SIGNUNDER.md §4.5, ADR 0004, ADR 0011).
//!
//! Two backends, chosen by the key reference:
//! - **Google Cloud KMS** (`projects/…/cryptoKeyVersions/N`), the production signer;
//! - **AWS KMS API** (`arn:aws:kms:…`), used only against the local-kms emulator in the
//!   local stack and CI. No AWS account is involved.

use std::collections::HashMap;
use std::sync::{Arc, Mutex};

use aws_sdk_kms::error::DisplayErrorContext;
use aws_sdk_kms::primitives::Blob;
use aws_sdk_kms::types::{MessageType, SigningAlgorithmSpec};
use c2pa::{CallbackSigner, SigningAlg};
use google_cloud_kms_v1::client::KeyManagementService;
use sha2::{Digest, Sha256};
use tokio::runtime::Handle;
use tokio::sync::RwLock;
use tonic::Status;
use x509_parser::pem::parse_x509_pem;

/// Which KMS holds a key, from its reference.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Backend {
    /// A Google Cloud KMS key version: `projects/P/locations/L/keyRings/R/cryptoKeys/K/cryptoKeyVersions/N`.
    GoogleCloud,
    /// An AWS KMS key ARN, as served by the local-kms emulator.
    AwsApi,
}

impl Backend {
    pub fn of(key_ref: &str) -> Option<Self> {
        if key_ref.starts_with("arn:aws:kms:") {
            return Some(Self::AwsApi);
        }
        let parts: Vec<&str> = key_ref.split('/').collect();
        let google = parts.len() == 10
            && parts[0] == "projects"
            && parts[2] == "locations"
            && parts[4] == "keyRings"
            && parts[6] == "cryptoKeys"
            && parts[8] == "cryptoKeyVersions"
            && parts.iter().all(|part| !part.is_empty());
        google.then_some(Self::GoogleCloud)
    }
}

/// KMS access shared by every request.
pub struct Kms {
    aws: aws_sdk_kms::Client,
    /// Built on first use, so a process that never sees a Google key needs no Google
    /// credentials, and rebuilt when Google stops accepting its access token.
    google: RwLock<Option<KeyManagementService>>,
    /// Public keys (DER SubjectPublicKeyInfo) by key reference. A key version's public key
    /// never changes, so entries are kept for the life of the process.
    public_keys: Mutex<HashMap<String, Vec<u8>>>,
}

impl Kms {
    /// AWS settings (for the emulator) come from the environment; Google credentials come
    /// from Application Default Credentials when a Google key is first used.
    pub async fn from_env() -> Self {
        let config = aws_config::load_defaults(aws_config::BehaviorVersion::latest()).await;
        Self {
            aws: aws_sdk_kms::Client::new(&config),
            google: RwLock::new(None),
            public_keys: Mutex::default(),
        }
    }

    async fn google(&self) -> Result<KeyManagementService, Status> {
        if let Some(client) = self.google.read().await.as_ref() {
            return Ok(client.clone());
        }
        let mut slot = self.google.write().await;
        if let Some(client) = slot.as_ref() {
            return Ok(client.clone());
        }
        let unavailable = |e: String| Status::unavailable(format!("Google Cloud KMS client: {e}"));
        let client = KeyManagementService::builder()
            .build()
            .await
            .map_err(|e| unavailable(e.to_string()))?;
        *slot = Some(client.clone());
        Ok(client)
    }

    /// Runs a Cloud KMS call. If Google rejects the access token, builds a new client
    /// and tries once more. The auth library times token expiry on a monotonic clock, so a
    /// process frozen and resumed (a suspended Fly.io machine) keeps offering an expired
    /// token; staging therefore stops the sidecar rather than suspending it (ADR 0012),
    /// and this is the safeguard if a token goes stale some other way.
    async fn with_google<T, F, Fut>(
        &self,
        call: &str,
        key_ref: &str,
        request: F,
    ) -> Result<T, Status>
    where
        F: Fn(KeyManagementService) -> Fut,
        Fut: Future<Output = Result<T, google_cloud_kms_v1::Error>>,
    {
        match request(self.google().await?).await {
            Err(e) if is_unauthenticated(&e) => {
                println!(
                    "Cloud KMS {call}: access token refused; building a new client and retrying"
                );
                *self.google.write().await = None;
                request(self.google().await?)
                    .await
                    .map_err(|e| google_status(call, key_ref, &e))
            }
            result => result.map_err(|e| google_status(call, key_ref, &e)),
        }
    }

    fn backend(key_ref: &str) -> Result<Backend, Status> {
        Backend::of(key_ref).ok_or_else(|| {
            Status::invalid_argument(format!("{key_ref:?} is not a KMS key reference"))
        })
    }

    /// The key's public half as DER SubjectPublicKeyInfo.
    pub async fn public_key(&self, key_ref: &str) -> Result<Vec<u8>, Status> {
        if let Some(key) = self.public_keys.lock().unwrap().get(key_ref) {
            return Ok(key.clone());
        }
        let key = match Self::backend(key_ref)? {
            Backend::AwsApi => {
                let reply = self
                    .aws
                    .get_public_key()
                    .key_id(key_ref)
                    .send()
                    .await
                    .map_err(|e| match e.as_service_error() {
                        Some(e) if e.is_not_found_exception() => {
                            Status::not_found(format!("KMS key {key_ref} does not exist"))
                        }
                        _ => Status::unavailable(format!(
                            "KMS GetPublicKey for {key_ref}: {}",
                            DisplayErrorContext(&e)
                        )),
                    })?;
                reply
                    .public_key
                    .map(Blob::into_inner)
                    .ok_or_else(|| Status::internal("KMS returned no public key"))?
            }
            Backend::GoogleCloud => {
                let reply = self
                    .with_google("GetPublicKey", key_ref, |client| async move {
                        client.get_public_key().set_name(key_ref).send().await
                    })
                    .await?;
                pem::parse(&reply.pem)
                    .map_err(|e| Status::internal(format!("Cloud KMS public key: {e}")))?
                    .into_contents()
            }
        };
        self.public_keys
            .lock()
            .unwrap()
            .insert(key_ref.to_owned(), key.clone());
        Ok(key)
    }

    /// Signs a SHA-256 digest with ECDSA P-256 and returns the DER-encoded signature.
    /// Both services take the digest rather than the data (AWS limits raw messages to
    /// 4096 bytes).
    pub async fn sign_digest(&self, key_ref: &str, digest: &[u8; 32]) -> Result<Vec<u8>, Status> {
        match Self::backend(key_ref)? {
            Backend::AwsApi => {
                let reply = self
                    .aws
                    .sign()
                    .key_id(key_ref)
                    .signing_algorithm(SigningAlgorithmSpec::EcdsaSha256)
                    .message_type(MessageType::Digest)
                    .message(Blob::new(digest.to_vec()))
                    .send()
                    .await
                    .map_err(|e| {
                        Status::unavailable(format!(
                            "KMS Sign with {key_ref}: {}",
                            DisplayErrorContext(&e)
                        ))
                    })?;
                reply
                    .signature
                    .map(Blob::into_inner)
                    .ok_or_else(|| Status::internal("KMS returned no signature"))
            }
            Backend::GoogleCloud => {
                let reply = self
                    .with_google("AsymmetricSign", key_ref, |client| async move {
                        client
                            .asymmetric_sign()
                            .set_name(key_ref)
                            .set_digest(
                                google_cloud_kms_v1::model::Digest::new()
                                    .set_sha256(digest.to_vec()),
                            )
                            .send()
                            .await
                    })
                    .await?;
                Ok(reply.signature.to_vec())
            }
        }
    }

    /// Builds a signer for the KMS key `key_ref`, presenting `cert_chain_pem` (signing
    /// certificate first), after checking that the certificate is for that key. The
    /// signer and this call block on `runtime`, so use both from a blocking thread.
    pub fn signer(
        self: &Arc<Self>,
        runtime: Handle,
        key_ref: String,
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
        Self::backend(&key_ref)?;

        let bad_chain = || {
            Status::invalid_argument(
                "cert_chain_pem must hold the PEM certificate chain for the KMS key",
            )
        };
        let (_, leaf) = parse_x509_pem(cert_chain_pem.as_bytes()).map_err(|_| bad_chain())?;
        let leaf = leaf.parse_x509().map_err(|_| bad_chain())?;
        if leaf.public_key().raw != runtime.block_on(self.public_key(&key_ref))? {
            return Err(Status::failed_precondition(format!(
                "the signing certificate is not for KMS key {key_ref}"
            )));
        }

        let kms = Arc::clone(self);
        let sign = move |_context: *const (), data: &[u8]| {
            let digest: [u8; 32] = Sha256::digest(data).into();
            // DER-encoded ECDSA; c2pa-rs converts it to the fixed-width form COSE wants.
            runtime
                .block_on(kms.sign_digest(&key_ref, &digest))
                .map_err(|status| {
                    c2pa::Error::OtherError(Box::new(std::io::Error::other(
                        status.message().to_owned(),
                    )))
                })
        };

        let signer = CallbackSigner::new(sign, SigningAlg::Es256, cert_chain_pem);
        Ok(match tsa_url {
            Some(url) => signer.set_tsa_url(url),
            None => signer,
        })
    }
}

fn is_unauthenticated(e: &google_cloud_kms_v1::Error) -> bool {
    e.status().map(|status| status.code)
        == Some(google_cloud_gax::error::rpc::Code::Unauthenticated)
}

/// A KMS key as the raw ES256 signer c2pa-rs uses for identity credentials. Blocks on
/// `runtime`, so use it from a blocking thread.
pub struct KmsRawSigner {
    kms: Arc<Kms>,
    runtime: Handle,
    key_ref: String,
}

impl c2pa_raw_crypto::RawSigner for KmsRawSigner {
    fn sign(&self, data: &[u8]) -> Result<Vec<u8>, c2pa_raw_crypto::RawSignerError> {
        let digest: [u8; 32] = Sha256::digest(data).into();
        let der = self
            .runtime
            .block_on(self.kms.sign_digest(&self.key_ref, &digest))
            .map_err(|status| {
                c2pa_raw_crypto::RawSignerError::CryptoLibraryError(status.message().to_owned())
            })?;
        // KMS returns DER; COSE wants the fixed-width r || s form.
        c2pa_raw_crypto::ec_utils::der_to_p1363(&der, 64)
    }

    fn alg(&self) -> SigningAlg {
        SigningAlg::Es256
    }

    fn max_signature_size(&self) -> usize {
        64
    }
}

impl Kms {
    /// A raw signer for the KMS key `key_ref`, after checking that the identity
    /// certificate first in `cert_chain_pem` is for that key. Blocks on `runtime`.
    pub fn identity_signer(
        self: &Arc<Self>,
        runtime: Handle,
        key_ref: String,
        algorithm: &str,
        cert_chain_pem: &str,
    ) -> Result<KmsRawSigner, Status> {
        if algorithm != "es256" {
            return Err(Status::invalid_argument(
                "identity keys sign with es256 only",
            ));
        }
        Self::backend(&key_ref)?;
        let bad_chain = || {
            Status::invalid_argument(
                "identity cert_chain_pem must hold the PEM certificate chain for the KMS key",
            )
        };
        let (_, leaf) = parse_x509_pem(cert_chain_pem.as_bytes()).map_err(|_| bad_chain())?;
        let leaf = leaf.parse_x509().map_err(|_| bad_chain())?;
        if leaf.public_key().raw != runtime.block_on(self.public_key(&key_ref))? {
            return Err(Status::failed_precondition(format!(
                "the identity certificate is not for KMS key {key_ref}"
            )));
        }
        Ok(KmsRawSigner {
            kms: Arc::clone(self),
            runtime,
            key_ref,
        })
    }
}

fn google_status(call: &str, key_ref: &str, e: &google_cloud_kms_v1::Error) -> Status {
    match e.status().map(|status| status.code) {
        Some(google_cloud_gax::error::rpc::Code::NotFound) => {
            Status::not_found(format!("Cloud KMS key {key_ref} does not exist"))
        }
        Some(google_cloud_gax::error::rpc::Code::PermissionDenied) => {
            Status::permission_denied(format!("Cloud KMS {call} on {key_ref}: permission denied"))
        }
        _ => Status::unavailable(format!("Cloud KMS {call} on {key_ref}: {e}")),
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

    /// Signs through a real Google Cloud KMS key version and verifies the signature with the
    /// key's public half. Set `SIGNUNDER_TEST_GCP_KEY` to the key version's resource name
    /// and log in with `gcloud auth application-default login`.
    #[test]
    #[ignore = "needs a Cloud KMS key version in SIGNUNDER_TEST_GCP_KEY"]
    fn google_cloud_key_signs_a_digest_that_verifies() {
        let key = std::env::var("SIGNUNDER_TEST_GCP_KEY").expect("SIGNUNDER_TEST_GCP_KEY");
        crate::internal_tls::install_crypto_provider();
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let kms = runtime.block_on(Kms::from_env());
        let message = b"SignUnder Cloud KMS test";
        let digest: [u8; 32] = Sha256::digest(message).into();

        let spki = runtime.block_on(kms.public_key(&key)).unwrap();
        let signature = runtime.block_on(kms.sign_digest(&key, &digest)).unwrap();

        let (_, spki) =
            <x509_parser::x509::SubjectPublicKeyInfo as x509_parser::prelude::FromDer<_>>::from_der(&spki)
                .unwrap();
        ring::signature::UnparsedPublicKey::new(
            &ring::signature::ECDSA_P256_SHA256_ASN1,
            spki.subject_public_key.data.as_ref(),
        )
        .verify(message, &signature)
        .expect("Cloud KMS's signature verifies with its public key");
    }

    #[test]
    fn key_references_choose_the_backend() {
        assert_eq!(Backend::of(KEY_ARN), Some(Backend::AwsApi));
        assert_eq!(
            Backend::of(
                "projects/signunder-prod/locations/europe-west3/keyRings/signunder/cryptoKeys/claim-signing/cryptoKeyVersions/1"
            ),
            Some(Backend::GoogleCloud)
        );
        for bad in [
            "dev:local",
            "projects/p/locations/l/keyRings/r/cryptoKeys/k",
            "projects//locations/l/keyRings/r/cryptoKeys/k/cryptoKeyVersions/1",
            "projects/p/locations/l/keyRings/r/cryptoKeys/k/cryptoKeyVersions/1/extra",
        ] {
            assert_eq!(Backend::of(bad), None, "{bad}");
        }
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
        let not_a_key = kms
            .signer(
                runtime.handle().clone(),
                "dev:local".into(),
                "es256",
                CHAIN,
                None,
            )
            .err()
            .unwrap();
        assert_eq!(not_a_key.code(), tonic::Code::InvalidArgument);
    }
}
