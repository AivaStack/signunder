//! CAWG identity assertions (`cawg.identity`, X.509 credentials, CAWG identity 1.1):
//! a statement inside the manifest, signed with a certificate naming the company that
//! publishes the content, while SignUnder's certificate still signs the claim
//! (ADR 0014).
//!
//! c2pa-rs offers `IdentityAssertionSigner` for this, but it signs the claim without a
//! timestamp. [`WithIdentity`] keeps the claim signer exactly as it was, timestamp
//! included, and only adds the identity assertions.

use std::sync::Arc;

use c2pa::dynamic_assertion::DynamicAssertion;
use c2pa::identity::builder::IdentityAssertionBuilder;
use c2pa::identity::x509::X509CredentialHolder;
use c2pa::{Signer, SigningAlg};
use c2pa_raw_crypto::RawSigner;
use tonic::Status;
use x509_parser::pem::Pem;

/// Assertions an identity assertion vouches for, besides the hard binding, which
/// c2pa-rs always includes.
const REFERENCED: &[&str] = &["c2pa.actions", "c2pa.actions.v2"];

/// A claim signer that also contributes identity assertions to the manifest.
pub struct WithIdentity {
    inner: Box<dyn Signer>,
    identities: Vec<Arc<IdentityAssertionBuilder>>,
}

impl WithIdentity {
    pub fn new(inner: Box<dyn Signer>, identities: Vec<IdentityAssertionBuilder>) -> Self {
        Self {
            inner,
            identities: identities.into_iter().map(Arc::new).collect(),
        }
    }
}

impl Signer for WithIdentity {
    fn sign(&self, data: &[u8]) -> c2pa::Result<Vec<u8>> {
        self.inner.sign(data)
    }

    fn alg(&self) -> SigningAlg {
        self.inner.alg()
    }

    fn certs(&self) -> c2pa::Result<Vec<Vec<u8>>> {
        self.inner.certs()
    }

    fn reserve_size(&self) -> usize {
        self.inner.reserve_size()
    }

    fn time_authority_url(&self) -> Option<String> {
        self.inner.time_authority_url()
    }

    fn timestamp_request_headers(&self) -> Option<Vec<(String, String)>> {
        self.inner.timestamp_request_headers()
    }

    fn timestamp_request_body(&self, message: &[u8]) -> c2pa::Result<Vec<u8>> {
        self.inner.timestamp_request_body(message)
    }

    fn send_timestamp_request(&self, message: &[u8]) -> Option<c2pa::Result<Vec<u8>>> {
        self.inner.send_timestamp_request(message)
    }

    fn ocsp_val(&self) -> Option<Vec<u8>> {
        self.inner.ocsp_val()
    }

    fn direct_cose_handling(&self) -> bool {
        self.inner.direct_cose_handling()
    }

    // Shared clones each time: c2pa-rs may ask more than once per manifest (once to
    // reserve space, once to write).
    fn dynamic_assertions(&self) -> Vec<Box<dyn DynamicAssertion>> {
        let mut assertions = self.inner.dynamic_assertions();
        assertions.extend(
            self.identities
                .iter()
                .map(|identity| Box::new(Arc::clone(identity)) as Box<dyn DynamicAssertion>),
        );
        assertions
    }
}

/// An identity assertion signed by `signer` with the certificate chain in
/// `cert_chain_pem` (the company's identity certificate first), naming `roles`.
pub fn assertion(
    signer: Box<dyn RawSigner + Send + Sync>,
    cert_chain_pem: &str,
    roles: &[String],
) -> Result<IdentityAssertionBuilder, Status> {
    let chain: Vec<Vec<u8>> = Pem::iter_from_buffer(cert_chain_pem.as_bytes())
        .map(|pem| pem.map(|pem| pem.contents))
        .collect::<Result<_, _>>()
        .map_err(|_| {
            Status::invalid_argument("identity cert_chain_pem is not a PEM certificate chain")
        })?;
    if chain.is_empty() {
        return Err(Status::invalid_argument(
            "identity cert_chain_pem holds no certificate",
        ));
    }
    let mut builder = IdentityAssertionBuilder::for_credential_holder(
        X509CredentialHolder::from_raw_signer(signer, chain),
    );
    builder.add_referenced_assertions(REFERENCED);
    let roles: Vec<&str> = roles.iter().map(String::as_str).collect();
    builder.add_roles(if roles.is_empty() {
        &["cawg.publisher"]
    } else {
        &roles
    });
    Ok(builder)
}

#[cfg(test)]
mod tests {
    use std::path::Path;

    use rcgen::{BasicConstraints, CertificateParams, DnType, IsCa, KeyPair, KeyUsagePurpose};

    use super::*;
    use crate::provenance::{self, DevSigner};

    const JPEG: &[u8] = include_bytes!("../tests/fixtures/sample.jpg");
    const DEFINITION: &str = r#"{
        "claim_generator_info": [{"name": "sign-sidecar tests", "version": "0.1.0"}],
        "title": "sample",
        "assertions": [{"label": "c2pa.actions", "data": {"actions": [{
            "action": "c2pa.created",
            "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
        }]}}]
    }"#;

    /// A throwaway identity CA and a certificate from it naming "Acme Images Ltd".
    fn identity() -> (String, String) {
        let ca_key = KeyPair::generate().unwrap();
        let mut ca = CertificateParams::new(Vec::<String>::new()).unwrap();
        ca.distinguished_name
            .push(DnType::CommonName, "Test Identity CA");
        ca.is_ca = IsCa::Ca(BasicConstraints::Unconstrained);
        ca.key_usages = vec![KeyUsagePurpose::KeyCertSign, KeyUsagePurpose::CrlSign];
        let ca_cert = ca.self_signed(&ca_key).unwrap();

        let key = KeyPair::generate().unwrap();
        // Exactly what the Identity CA issues.
        let leaf = crate::identity_ca::leaf_params("Acme Images Ltd", "acme.example", 365).unwrap();
        let issuer = rcgen::Issuer::from_params(&ca, &ca_key);
        let cert = leaf.signed_by(&key, &issuer).unwrap();
        (
            format!("{}{}", cert.pem(), ca_cert.pem()),
            key.serialize_pem(),
        )
    }

    #[test]
    fn the_manifest_carries_an_identity_assertion_naming_the_publisher() {
        let (chain, key) = identity();
        let raw =
            c2pa_raw_crypto::signer_from_private_key(key.as_bytes(), SigningAlg::Es256).unwrap();
        let dev =
            DevSigner::load(&Path::new(env!("CARGO_MANIFEST_DIR")).join("dev-signer")).unwrap();
        let signer = WithIdentity::new(
            dev.signer(None).unwrap(),
            vec![assertion(raw, &chain, &["cawg.publisher".into()]).unwrap()],
        );

        let signed = provenance::sign(&signer, JPEG, "image/jpeg", DEFINITION).unwrap();
        let report: serde_json::Value = serde_json::from_str(&signed.report_json).unwrap();
        let manifest = &report["manifests"][0];

        let identity = manifest["assertions"]
            .as_object()
            .unwrap()
            .iter()
            .find(|(label, _)| label.starts_with("cawg.identity"))
            .map(|(_, value)| value)
            .expect("a cawg.identity assertion");
        assert_eq!(identity["signer_payload"]["role"][0], "cawg.publisher");
        assert_eq!(
            identity["signature"]["certificateInfo"]["subject"]["O"],
            "Acme Images Ltd"
        );
        assert_eq!(identity["signer_payload"]["sig_type"], "cawg.x509.cose");

        let codes = |outcome: &str| -> Vec<String> {
            manifest["validationResults"][outcome]
                .as_array()
                .map(|statuses| {
                    statuses
                        .iter()
                        .map(|s| s["code"].as_str().unwrap().to_owned())
                        .collect()
                })
                .unwrap_or_default()
        };
        assert!(codes("success").contains(&"cawg.x509.signature.validated".to_owned()));
        assert!(codes("success").contains(&"cawg.identity.well-formed".to_owned()));
        // A test CA is on no trust list; nothing else may fail.
        assert_eq!(
            codes("failure"),
            [
                "signingCredential.untrusted",
                "cawg.x509.credential.untrusted"
            ]
        );
    }

    #[test]
    fn a_chain_that_is_not_pem_is_refused() {
        let (_, key) = identity();
        let raw =
            c2pa_raw_crypto::signer_from_private_key(key.as_bytes(), SigningAlg::Es256).unwrap();
        let Err(refused) = assertion(raw, "not a certificate", &[]) else {
            panic!("a chain that is not PEM was accepted");
        };
        assert_eq!(refused.code(), tonic::Code::InvalidArgument);
    }
}
