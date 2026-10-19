//! A KMS key as an rcgen signing key, so certificates and requests can be signed
//! without the private key leaving KMS (used by kms-csr and identity-ca).

use std::sync::Arc;

use rcgen::{PublicKeyData, SignatureAlgorithm, SigningKey, SubjectPublicKeyInfo};
use sha2::{Digest, Sha256};
use tokio::runtime::Handle;

use crate::kms::Kms;

/// An ECDSA P-256 key in KMS. rcgen calls [`SigningKey::sign`] synchronously, so use
/// it from a blocking thread: it blocks on the runtime it was loaded on.
pub struct KmsKey {
    kms: Arc<Kms>,
    runtime: Handle,
    key_ref: String,
    public_key: SubjectPublicKeyInfo,
}

impl KmsKey {
    /// Reads the key's public half and checks it is ECDSA P-256.
    pub async fn load(kms: Arc<Kms>, key_ref: String) -> Result<Self, String> {
        let spki = kms
            .public_key(&key_ref)
            .await
            .map_err(|status| status.message().to_owned())?;
        let public_key = SubjectPublicKeyInfo::from_der(&spki).map_err(|e| e.to_string())?;
        if public_key.algorithm() != &rcgen::PKCS_ECDSA_P256_SHA256 {
            return Err(format!(
                "{key_ref} must be ECDSA P-256 (Cloud KMS EC_SIGN_P256_SHA256)"
            ));
        }
        Ok(Self {
            kms,
            runtime: Handle::current(),
            key_ref,
            public_key,
        })
    }
}

impl PublicKeyData for KmsKey {
    fn der_bytes(&self) -> &[u8] {
        self.public_key.der_bytes()
    }

    fn algorithm(&self) -> &'static SignatureAlgorithm {
        self.public_key.algorithm()
    }
}

impl SigningKey for KmsKey {
    fn sign(&self, message: &[u8]) -> Result<Vec<u8>, rcgen::Error> {
        let digest: [u8; 32] = Sha256::digest(message).into();
        // DER-encoded ECDSA, which is what rcgen writes for ECDSA_P256_SHA256.
        self.runtime
            .block_on(self.kms.sign_digest(&self.key_ref, &digest))
            .map_err(|_| rcgen::Error::RemoteKeyError)
    }
}
