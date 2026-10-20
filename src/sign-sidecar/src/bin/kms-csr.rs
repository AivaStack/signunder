//! Writes a certificate signing request for a C2PA claim signing key held in a KMS.
//! KMS makes the request's signature, so the private key never leaves it (ADR 0004).
//!
//! The request follows the C2PA Conformance Program's claim-signing leaf AL1 request
//! profile (`claimSigningLeaf.al1.csr.schema.json`, program documents v0.1; the v0.2
//! documents publish no schema): subject C, O and CN; basic constraints (critical, not a
//! CA); key usage (critical, digitalSignature and contentCommitment); extended key usage
//! (C2PA claim signing and email protection); the C2PA certificate policy; the AL1
//! assurance level; and the product's CPL record id.
//!
//! ```text
//! kms-csr --key KEY --common-name CN --organization O --country CC
//!         [--cpl-record-id ID] [--out FILE]
//! ```
//!
//! KEY is a Google Cloud KMS key version (`projects/…/cryptoKeyVersions/N`) or, for the
//! local-kms emulator, a key ARN. Credentials come from the environment, as for the sidecar:
//! Application Default Credentials for Google Cloud.

use std::process::ExitCode;

use std::sync::Arc;

use rcgen::{
    CertificateParams, CustomExtension, DnType, ExtendedKeyUsagePurpose, IsCa, KeyUsagePurpose,
};
use sign_sidecar::kms::Kms;
use sign_sidecar::kms_key::KmsKey;

/// OIDs from the C2PA Certificate Policy and the request profile.
const C2PA_KP_CLAIM_SIGNING: &[u64] = &[1, 3, 6, 1, 4, 1, 62558, 2, 1];
const C2PA_CERTIFICATE_POLICY: &[u64] = &[1, 3, 6, 1, 4, 1, 62558, 1, 1];
const C2PA_ASSURANCE_LEVEL: &[u64] = &[1, 3, 6, 1, 4, 1, 62558, 3];
const C2PA_ASSURANCE_LEVEL_1: &[u64] = &[1, 3, 6, 1, 4, 1, 62558, 3, 10];
const C2PA_CPL_RECORD_ID: &[u64] = &[1, 3, 6, 1, 4, 1, 62558, 4];
const CERTIFICATE_POLICIES: &[u64] = &[2, 5, 29, 32];

struct Subject {
    common_name: String,
    organization: String,
    country: String,
    cpl_record_id: Option<String>,
}

/// The request's contents, independent of who signs it.
fn request_params(subject: &Subject) -> Result<CertificateParams, String> {
    for (field, value) in [
        ("common name", &subject.common_name),
        ("organization", &subject.organization),
        ("country", &subject.country),
    ] {
        // Certificate Policy: "All values of the DN SHALL be in plain ASCII text".
        if value.is_empty() || !value.is_ascii() {
            return Err(format!("the {field} must be plain, non-empty ASCII"));
        }
    }
    if subject.country.len() != 2 || !subject.country.chars().all(|c| c.is_ascii_uppercase()) {
        return Err("the country must be an ISO 3166-1 alpha-2 code, such as IE".into());
    }

    let mut params = CertificateParams::new(Vec::<String>::new()).map_err(|e| e.to_string())?;
    // rcgen's defaults include a common name; start empty so the order is C, O, CN.
    params.distinguished_name = rcgen::DistinguishedName::new();
    params
        .distinguished_name
        .push(DnType::CountryName, &subject.country);
    params
        .distinguished_name
        .push(DnType::OrganizationName, &subject.organization);
    params
        .distinguished_name
        .push(DnType::CommonName, &subject.common_name);
    params.is_ca = IsCa::ExplicitNoCa;
    params.key_usages = vec![
        KeyUsagePurpose::DigitalSignature,
        KeyUsagePurpose::ContentCommitment,
    ];
    params.extended_key_usages = vec![
        ExtendedKeyUsagePurpose::Other(C2PA_KP_CLAIM_SIGNING.to_vec()),
        ExtendedKeyUsagePurpose::EmailProtection,
    ];

    // certificatePolicies ::= SEQUENCE OF PolicyInformation { policyIdentifier }
    let policies = yasna::construct_der(|w| {
        w.write_sequence(|w| {
            w.next().write_sequence(|w| {
                w.next()
                    .write_oid(&yasna::models::ObjectIdentifier::from_slice(
                        C2PA_CERTIFICATE_POLICY,
                    ));
            })
        })
    });
    let assurance_level = yasna::construct_der(|w| {
        w.write_oid(&yasna::models::ObjectIdentifier::from_slice(
            C2PA_ASSURANCE_LEVEL_1,
        ))
    });
    params.custom_extensions = vec![
        CustomExtension::from_oid_content(CERTIFICATE_POLICIES, policies),
        CustomExtension::from_oid_content(C2PA_ASSURANCE_LEVEL, assurance_level),
    ];
    if let Some(id) = &subject.cpl_record_id {
        if id.len() != 36 {
            return Err("the CPL record id is 36 characters".into());
        }
        let value = yasna::construct_der(|w| w.write_utf8_string(id));
        params
            .custom_extensions
            .push(CustomExtension::from_oid_content(C2PA_CPL_RECORD_ID, value));
    }
    Ok(params)
}

async fn csr(kms: Arc<Kms>, key_ref: String, subject: &Subject) -> Result<String, String> {
    let params = request_params(subject)?;
    let key = KmsKey::load(kms, key_ref).await?;
    tokio::task::spawn_blocking(move || {
        params
            .serialize_request(&key)
            .and_then(|request| request.pem())
            .map_err(|e| format!("building the request: {e}"))
    })
    .await
    .map_err(|e| e.to_string())?
}

fn parse_args() -> Result<(String, Subject, Option<String>), String> {
    let mut args = std::env::args().skip(1);
    let (mut key_arn, mut cn, mut o, mut c, mut cpl, mut out) =
        (None, None, None, None, None, None);
    while let Some(flag) = args.next() {
        let value = args.next().ok_or_else(|| format!("{flag} needs a value"))?;
        match flag.as_str() {
            // --key-arn is the older name, from when the only backend was AWS's API.
            "--key" | "--key-arn" => key_arn = Some(value),
            "--common-name" => cn = Some(value),
            "--organization" => o = Some(value),
            "--country" => c = Some(value),
            "--cpl-record-id" => cpl = Some(value),
            "--out" => out = Some(value),
            _ => return Err(format!("unknown argument {flag}")),
        }
    }
    let need = |v: Option<String>, flag: &str| v.ok_or_else(|| format!("{flag} is required"));
    Ok((
        need(key_arn, "--key")?,
        Subject {
            common_name: need(cn, "--common-name")?,
            organization: need(o, "--organization")?,
            country: need(c, "--country")?,
            cpl_record_id: cpl,
        },
        out,
    ))
}

#[tokio::main]
async fn main() -> ExitCode {
    let _ = rustls::crypto::ring::default_provider().install_default();
    let (key_arn, subject, out) = match parse_args() {
        Ok(args) => args,
        Err(e) => {
            eprintln!("kms-csr: {e}");
            return ExitCode::from(2);
        }
    };
    if subject.cpl_record_id.is_none() {
        eprintln!(
            "kms-csr: warning: no --cpl-record-id; the program's profile requires it, so this \
             request is only good for testing"
        );
    }
    match csr(Arc::new(Kms::from_env().await), key_arn, &subject).await {
        Ok(pem) => {
            match out {
                Some(path) => {
                    if let Err(e) = std::fs::write(&path, pem) {
                        eprintln!("kms-csr: writing {path}: {e}");
                        return ExitCode::FAILURE;
                    }
                }
                None => print!("{pem}"),
            }
            ExitCode::SUCCESS
        }
        Err(e) => {
            eprintln!("kms-csr: {e}");
            ExitCode::FAILURE
        }
    }
}

#[cfg(test)]
mod tests {
    use sha2::Digest;
    use x509_parser::prelude::FromDer;

    use super::*;

    fn subject(cpl: Option<&str>) -> Subject {
        Subject {
            common_name: "SignUnder".into(),
            organization: "Example Ltd".into(),
            country: "IE".into(),
            cpl_record_id: cpl.map(str::to_owned),
        }
    }

    #[test]
    fn subject_fields_are_checked() {
        let mut bad = subject(None);
        bad.country = "Ireland".into();
        assert!(request_params(&bad).is_err());
        let mut bad = subject(None);
        bad.organization = "Société".into();
        assert!(request_params(&bad).is_err());
        assert!(request_params(&subject(Some("too-short"))).is_err());
    }

    /// Writes a request with the given key, and returns openssl's text dump of it after
    /// openssl has verified its signature.
    fn openssl_text(pem: &str) -> String {
        let dir = std::env::temp_dir().join(format!("kms-csr-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let path = dir.join(format!("{:x}.csr", sha2::Sha256::digest(pem.as_bytes())[0]));
        std::fs::write(&path, pem).unwrap();
        let output = std::process::Command::new("openssl")
            .args(["req", "-verify", "-noout", "-text", "-in"])
            .arg(&path)
            .output()
            .expect("openssl is installed");
        assert!(
            output.status.success(),
            "{}",
            String::from_utf8_lossy(&output.stderr)
        );
        // OpenSSL 3.0 writes "C = IE"; later versions write "C=IE".
        String::from_utf8(output.stdout).unwrap().replace(" = ", "=")
    }

    fn assert_profile(text: &str) {
        assert!(
            text.contains("Subject: C=IE, O=Example Ltd, CN=SignUnder"),
            "{text}"
        );
        assert!(
            text.contains("X509v3 Basic Constraints: critical\n                    CA:FALSE"),
            "{text}"
        );
        assert!(
            text.contains(
                "X509v3 Key Usage: critical\n                    Digital Signature, Non Repudiation"
            ),
            "{text}"
        );
        assert!(text.contains("1.3.6.1.4.1.62558.2.1"), "{text}");
        assert!(text.contains("E-mail Protection"), "{text}");
        assert!(text.contains("Policy: 1.3.6.1.4.1.62558.1.1"), "{text}");
        assert!(text.contains("1.3.6.1.4.1.62558.3:"), "{text}");
        assert!(text.contains("ecdsa-with-SHA256"), "{text}");
    }

    #[test]
    fn request_follows_the_al1_profile() {
        let key = rcgen::KeyPair::generate_for(&rcgen::PKCS_ECDSA_P256_SHA256).unwrap();
        let cpl = "0123456789abcdef0123456789abcdef0123";
        let pem = request_params(&subject(Some(cpl)))
            .unwrap()
            .serialize_request(&key)
            .unwrap()
            .pem()
            .unwrap();

        let text = openssl_text(&pem);
        assert_profile(&text);
        assert!(text.contains("1.3.6.1.4.1.62558.4:"), "{text}");
        assert!(text.contains(cpl), "{text}");
    }

    /// The development key held by the local-kms emulator (dev-signer/local-kms-seed.yaml).
    const DEV_KEY_ARN: &str =
        "arn:aws:kms:eu-central-1:111122223333:key/00000000-0000-4000-8000-00005160de01";

    #[test]
    #[ignore = "needs the local-kms emulator: make test-kms"]
    fn kms_signs_a_request_for_its_own_key() {
        let _ = rustls::crypto::ring::default_provider().install_default();
        let runtime = tokio::runtime::Runtime::new().unwrap();
        let pem = runtime.block_on(async {
            csr(
                Arc::new(Kms::from_env().await),
                DEV_KEY_ARN.into(),
                &subject(None),
            )
            .await
            .unwrap()
        });

        // openssl verifies the signature against the public key in the request, so this
        // proves KMS signed with the key whose public half the request carries.
        assert_profile(&openssl_text(&pem));

        // And that public key is the development key's.
        let dev_chain = include_str!("../../dev-signer/es256.chain.pem");
        let leaf = x509_parser::pem::parse_x509_pem(dev_chain.as_bytes())
            .unwrap()
            .1;
        let leaf = leaf.parse_x509().unwrap();
        let request = x509_parser::pem::parse_x509_pem(pem.as_bytes()).unwrap().1;
        let (_, request) = x509_parser::certification_request::X509CertificationRequest::from_der(
            &request.contents,
        )
        .unwrap();
        assert_eq!(
            request.certification_request_info.subject_pki.raw,
            leaf.public_key().raw
        );
    }
}
