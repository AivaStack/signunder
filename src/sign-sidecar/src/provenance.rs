//! Signing and validation through c2pa-rs. Everything here blocks; call it from
//! `spawn_blocking`.

use std::io::Cursor;
use std::path::Path;

use c2pa::{Builder, Context, Reader, SigningAlg, ValidationState, create_signer};
use tonic::Status;

/// `SignerRef.key_ref` that selects the development signer.
pub const DEV_KEY_REF: &str = "dev:local";

const DEV_CHAIN_FILE: &str = "es256.chain.pem";
const DEV_KEY_FILE: &str = "es256.key.pem";

/// The non-conformant development signer (see `dev-signer/README.md`). Its private key
/// is public, so it is only loaded when `SIGNUNDER_DEV_SIGNER_DIR` is set.
pub struct DevSigner {
    chain_pem: Vec<u8>,
    key_pem: Vec<u8>,
}

impl DevSigner {
    pub fn load(dir: &Path) -> std::io::Result<Self> {
        Ok(Self {
            chain_pem: std::fs::read(dir.join(DEV_CHAIN_FILE))?,
            key_pem: std::fs::read(dir.join(DEV_KEY_FILE))?,
        })
    }
}

pub struct Signed {
    pub asset: Vec<u8>,
    pub manifest_store: Vec<u8>,
    pub report_json: String,
}

fn context() -> Result<Context, Status> {
    Context::new()
        .with_settings(r#"{"verify": {"remote_manifest_fetch": false}}"#)
        .map_err(internal)
}

fn internal(e: c2pa::Error) -> Status {
    Status::internal(format!("c2pa: {e}"))
}

fn invalid(e: c2pa::Error) -> Status {
    Status::invalid_argument(format!("c2pa: {e}"))
}

/// Signs `asset` with the development signer and validates the result before returning it.
pub fn sign(
    dev: &DevSigner,
    asset: &[u8],
    mime_type: &str,
    manifest_definition_json: &str,
    tsa_url: Option<String>,
) -> Result<Signed, Status> {
    let signer = create_signer::from_keys(&dev.chain_pem, &dev.key_pem, SigningAlg::Es256, tsa_url)
        .map_err(internal)?;

    let mut builder = Builder::from_context(context()?)
        .with_definition(manifest_definition_json)
        .map_err(invalid)?;

    let mut source = Cursor::new(asset);
    let mut dest = Cursor::new(Vec::with_capacity(asset.len() + 32 * 1024));
    let manifest_store = builder
        .sign(signer.as_ref(), mime_type, &mut source, &mut dest)
        .map_err(|e| match e {
            c2pa::Error::UnsupportedType | c2pa::Error::InvalidAsset(_) => invalid(e),
            e => internal(e),
        })?;
    let asset = dest.into_inner();

    // Nothing leaves the sidecar unverified (SIGNUNDER.md §4.7).
    let (state, report_json) = validate(&asset, mime_type)?;
    if state == ValidationState::Invalid {
        return Err(Status::internal(format!(
            "signed asset failed validation: {report_json}"
        )));
    }

    Ok(Signed {
        asset,
        manifest_store,
        report_json,
    })
}

/// Reads and validates the manifest store in `asset`, returning c2pa-rs's report.
pub fn validate(asset: &[u8], mime_type: &str) -> Result<(ValidationState, String), Status> {
    let reader = Reader::from_context(context()?)
        .with_stream(mime_type, Cursor::new(asset))
        .map_err(|e| match e {
            c2pa::Error::JumbfNotFound => Status::not_found("asset has no C2PA manifest"),
            e => invalid(e),
        })?;
    Ok((reader.validation_state(), reader.json()))
}

#[cfg(test)]
mod tests {
    use super::*;

    const JPEG: &[u8] = include_bytes!("../tests/fixtures/sample.jpg");
    const PNG: &[u8] = include_bytes!("../tests/fixtures/sample.png");

    const DEFINITION: &str = r#"{
        "claim_generator_info": [{"name": "sign-sidecar tests", "version": "0.1.0"}],
        "title": "sample",
        "assertions": [{
            "label": "c2pa.actions",
            "data": {"actions": [{
                "action": "c2pa.created",
                "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
            }]}
        }]
    }"#;

    fn dev() -> DevSigner {
        DevSigner::load(&Path::new(env!("CARGO_MANIFEST_DIR")).join("dev-signer")).unwrap()
    }

    fn codes<'a>(report: &'a serde_json::Value, outcome: &str) -> Vec<&'a str> {
        report["validation_results"]["activeManifest"][outcome]
            .as_array()
            .unwrap()
            .iter()
            .map(|status| status["code"].as_str().unwrap())
            .collect()
    }

    fn signs_and_validates(original: &[u8], mime_type: &str) {
        let signed = sign(&dev(), original, mime_type, DEFINITION, None).unwrap();
        assert!(signed.asset.len() > original.len());
        assert!(!signed.manifest_store.is_empty());

        // Read the manifest back from the signed bytes alone.
        let (state, report_json) = validate(&signed.asset, mime_type).unwrap();
        assert_eq!(report_json, signed.report_json);
        let report: serde_json::Value = serde_json::from_str(&report_json).unwrap();

        // Well formed and cryptographically intact, but not Trusted: the development
        // certificate does not chain to the C2PA trust list.
        assert_eq!(state, ValidationState::Valid);
        assert_eq!(report["validation_state"], "Valid");
        let success = codes(&report, "success");
        assert!(success.contains(&"claimSignature.validated"));
        assert!(success.contains(&"claimSignature.insideValidity"));
        assert!(success.contains(&"assertion.dataHash.match"));
        assert_eq!(codes(&report, "failure"), ["signingCredential.untrusted"]);

        let manifest = &report["manifests"][report["active_manifest"].as_str().unwrap()];
        assert_eq!(manifest["signature_info"]["alg"], "Es256");
        assert_eq!(
            manifest["signature_info"]["issuer"],
            "SignUnder DEV - NON-CONFORMANT"
        );
        assert_eq!(manifest["assertions"][0]["label"], "c2pa.actions.v2");
        assert_eq!(
            manifest["assertions"][0]["data"]["actions"][0]["action"],
            "c2pa.created"
        );
    }

    fn detects_tampering(original: &[u8], mime_type: &str) {
        let mut tampered = sign(&dev(), original, mime_type, DEFINITION, None)
            .unwrap()
            .asset;
        // The manifest is embedded near the start of both formats, so this is image data.
        let i = tampered.len() - 16;
        tampered[i] ^= 0xFF;

        let (state, report_json) = validate(&tampered, mime_type).unwrap();
        let report: serde_json::Value = serde_json::from_str(&report_json).unwrap();
        assert_eq!(state, ValidationState::Invalid);
        assert!(codes(&report, "failure").contains(&"assertion.dataHash.mismatch"));
        assert!(!codes(&report, "success").contains(&"assertion.dataHash.match"));
    }

    #[test]
    fn jpeg_signs_and_validates() {
        signs_and_validates(JPEG, "image/jpeg");
    }

    #[test]
    fn png_signs_and_validates() {
        signs_and_validates(PNG, "image/png");
    }

    #[test]
    fn jpeg_tampering_is_detected() {
        detects_tampering(JPEG, "image/jpeg");
    }

    #[test]
    fn png_tampering_is_detected() {
        detects_tampering(PNG, "image/png");
    }

    #[test]
    fn unsigned_asset_has_no_manifest() {
        let status = validate(JPEG, "image/jpeg").unwrap_err();
        assert_eq!(status.code(), tonic::Code::NotFound);
    }

    #[test]
    fn bad_manifest_definition_is_rejected() {
        let status = sign(&dev(), JPEG, "image/jpeg", "not json", None)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }

    #[test]
    fn unparseable_asset_is_rejected() {
        let status = sign(&dev(), b"not a jpeg", "image/jpeg", DEFINITION, None)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }

    #[test]
    fn unsupported_format_is_rejected() {
        let status = sign(&dev(), b"plain text", "text/plain", DEFINITION, None)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }
}
