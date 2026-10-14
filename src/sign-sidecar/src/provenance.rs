//! Signing and validation through c2pa-rs. Everything here blocks; call it from
//! `spawn_blocking`.

use std::io::Cursor;
use std::path::Path;

use base64::Engine;
use c2pa::assertions::{SoftBinding, SoftBindingBlock, labels::SOFT_BINDING};
use c2pa::{
    BoxedSigner, Builder, Context, Reader, Signer, SigningAlg, ValidationState, create_signer,
};
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

    pub fn signer(&self, tsa_url: Option<String>) -> Result<BoxedSigner, Status> {
        create_signer::from_keys(&self.chain_pem, &self.key_pem, SigningAlg::Es256, tsa_url)
            .map_err(internal)
    }
}

pub struct Signed {
    pub asset: Vec<u8>,
    pub manifest_store: Vec<u8>,
    pub state: ValidationState,
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

/// Signs `asset` and validates the result before returning it.
pub fn sign(
    signer: &dyn Signer,
    asset: &[u8],
    mime_type: &str,
    manifest_definition_json: &str,
) -> Result<Signed, Status> {
    let (definition, soft_bindings) = take_soft_bindings(manifest_definition_json)?;
    let mut builder = Builder::from_context(context()?)
        .with_definition(definition.as_str())
        .map_err(invalid)?;
    for (soft_binding, created) in &soft_bindings {
        builder
            .add_assertion(SOFT_BINDING, soft_binding)
            .map_err(invalid)?;
        // add_assertion files it as gathered; keep the definition's "created" flag.
        if let Some(added) = builder.definition.assertions.last_mut() {
            added.created = *created;
        }
    }

    let mut source = Cursor::new(asset);
    let mut dest = Cursor::new(Vec::with_capacity(asset.len() + 32 * 1024));
    let manifest_store = builder
        .sign(signer, mime_type, &mut source, &mut dest)
        .map_err(|e| match e {
            // InvalidManifest: c2pa-rs's own check of what it just signed, which a bad
            // manifest definition fails.
            c2pa::Error::UnsupportedType
            | c2pa::Error::InvalidAsset(_)
            | c2pa::Error::InvalidManifest(_) => invalid(e),
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
        state,
        report_json,
    })
}

/// Takes the `c2pa.soft-binding` assertions out of a manifest definition so they can be
/// added as typed assertions. A soft binding's block values are bytes; left in the JSON
/// definition they would be stored as text. In the definition they are base64.
fn take_soft_bindings(definition: &str) -> Result<(String, Vec<(SoftBinding, bool)>), Status> {
    let bad = |what: &str| Status::invalid_argument(format!("{SOFT_BINDING}: {what}"));

    // Anything that is not a JSON object is left for c2pa-rs to reject.
    let Ok(mut root) = serde_json::from_str::<serde_json::Value>(definition) else {
        return Ok((definition.to_owned(), Vec::new()));
    };
    let Some(assertions) = root.get_mut("assertions").and_then(|a| a.as_array_mut()) else {
        return Ok((definition.to_owned(), Vec::new()));
    };

    let mut soft_bindings = Vec::new();
    let mut kept = Vec::with_capacity(assertions.len());
    for assertion in assertions.drain(..) {
        if assertion["label"] != SOFT_BINDING {
            kept.push(assertion);
            continue;
        }
        let data = &assertion["data"];
        let alg = data["alg"].as_str().ok_or_else(|| bad("alg is required"))?;
        let blocks = data["blocks"]
            .as_array()
            .filter(|blocks| !blocks.is_empty())
            .ok_or_else(|| bad("blocks is required"))?;
        let blocks = blocks
            .iter()
            .map(|block| {
                if block["scope"]
                    .as_object()
                    .is_some_and(|scope| !scope.is_empty())
                {
                    return Err(bad("block scopes are not supported"));
                }
                let value = block["value"]
                    .as_str()
                    .and_then(|v| base64::engine::general_purpose::STANDARD.decode(v).ok())
                    .ok_or_else(|| bad("block value must be base64"))?;
                Ok(SoftBindingBlock {
                    scope: Default::default(),
                    value,
                })
            })
            .collect::<Result<_, _>>()?;
        // The padding fields are private, so this cannot be a struct literal.
        let mut soft_binding = SoftBinding::default();
        soft_binding.alg = Some(alg.to_owned());
        soft_binding.blocks = blocks;
        soft_bindings.push((soft_binding, assertion["created"] == true));
    }
    *assertions = kept;
    Ok((root.to_string(), soft_bindings))
}

/// Reads and validates the manifest store in `asset`. The report is crJSON, the format
/// the C2PA conformance program takes as evidence. It carries each manifest's validation
/// results but no overall state, so that is returned beside it.
pub fn validate(asset: &[u8], mime_type: &str) -> Result<(ValidationState, String), Status> {
    let reader = Reader::from_context(context()?)
        .with_stream(mime_type, Cursor::new(asset))
        .map_err(|e| match e {
            c2pa::Error::JumbfNotFound => Status::not_found("asset has no C2PA manifest"),
            e => invalid(e),
        })?;
    let report_json = reader.crjson_checked().map_err(internal)?;
    Ok((reader.validation_state(), report_json))
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

    fn dev() -> BoxedSigner {
        DevSigner::load(&Path::new(env!("CARGO_MANIFEST_DIR")).join("dev-signer"))
            .unwrap()
            .signer(None)
            .unwrap()
    }

    fn codes<'a>(report: &'a serde_json::Value, outcome: &str) -> Vec<&'a str> {
        report["manifests"][0]["validationResults"][outcome]
            .as_array()
            .unwrap()
            .iter()
            .map(|status| status["code"].as_str().unwrap())
            .collect()
    }

    fn signs_and_validates(original: &[u8], mime_type: &str) {
        let signed = sign(dev().as_ref(), original, mime_type, DEFINITION).unwrap();
        assert!(signed.asset.len() > original.len());
        assert!(!signed.manifest_store.is_empty());

        // Read the manifest back from the signed bytes alone.
        let (state, report_json) = validate(&signed.asset, mime_type).unwrap();
        let report: serde_json::Value = serde_json::from_str(&report_json).unwrap();

        // Well formed and cryptographically intact, but not Trusted: the development
        // certificate does not chain to the C2PA trust list.
        assert_eq!(state, ValidationState::Valid);
        assert_eq!(report["manifests"].as_array().unwrap().len(), 1);
        let success = codes(&report, "success");
        assert!(success.contains(&"claimSignature.validated"));
        assert!(success.contains(&"claimSignature.insideValidity"));
        assert!(success.contains(&"assertion.dataHash.match"));
        assert_eq!(codes(&report, "failure"), ["signingCredential.untrusted"]);

        let manifest = &report["manifests"][0];
        assert_eq!(manifest["signature"]["algorithm"], "es256");
        assert_eq!(
            manifest["signature"]["certificateInfo"]["subject"]["O"],
            "SignUnder DEV - NON-CONFORMANT"
        );
        assert_eq!(
            manifest["assertions"]["c2pa.actions.v2"]["actions"][0]["action"],
            "c2pa.created"
        );
        assert!(manifest["assertions"]["c2pa.hash.data"].is_object());

        // The report returned with the signed asset describes the same manifest.
        let at_signing: serde_json::Value = serde_json::from_str(&signed.report_json).unwrap();
        assert_eq!(at_signing["manifests"][0]["label"], manifest["label"]);
    }

    fn detects_tampering(original: &[u8], mime_type: &str) {
        let mut tampered = sign(dev().as_ref(), original, mime_type, DEFINITION)
            .unwrap()
            .asset;
        // Flip a byte of image data: find a run of the original's middle in the signed file,
        // wherever the format put the manifest.
        let middle = &original[original.len() / 2..original.len() / 2 + 16];
        let i = tampered
            .windows(middle.len())
            .position(|window| window == middle)
            .expect("the image data is still in the signed file");
        tampered[i + 8] ^= 0xFF;

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

    const WEBP: &[u8] = include_bytes!("../tests/fixtures/sample.webp");

    #[test]
    fn webp_signs_and_validates() {
        signs_and_validates(WEBP, "image/webp");
    }

    #[test]
    fn webp_tampering_is_detected() {
        detects_tampering(WEBP, "image/webp");
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
        let status = sign(dev().as_ref(), JPEG, "image/jpeg", "not json")
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }

    fn with_soft_binding(data: &str) -> String {
        let mut definition: serde_json::Value = serde_json::from_str(DEFINITION).unwrap();
        definition["assertions"].as_array_mut().unwrap().push(
            serde_json::json!({"label": "c2pa.soft-binding", "data": serde_json::from_str::<serde_json::Value>(data).unwrap()}),
        );
        definition.to_string()
    }

    #[test]
    fn soft_binding_value_is_stored_as_bytes() {
        let definition = with_soft_binding(
            r#"{"alg": "com.adobe.trustmark.Q", "blocks": [{"scope": {}, "value": "AAECAwQFBgc="}]}"#,
        );

        let signed = sign(dev().as_ref(), PNG, "image/png", &definition).unwrap();

        assert_eq!(signed.state, ValidationState::Valid);
        let report: serde_json::Value = serde_json::from_str(&signed.report_json).unwrap();
        let assertions = &report["manifests"][0]["assertions"];
        let soft_binding = &assertions["c2pa.soft-binding"];
        assert_eq!(soft_binding["alg"], "com.adobe.trustmark.Q");
        assert_eq!(
            soft_binding["blocks"][0]["value"],
            serde_json::json!([0, 1, 2, 3, 4, 5, 6, 7])
        );
        // crJSON prints a byte string and an array of numbers alike, so look at the CBOR:
        // the key "value", then a byte string of length 8 (0x48). An array would be 0x88.
        let encoded = b"\x65value\x48\x00\x01\x02\x03\x04\x05\x06\x07";
        assert!(
            signed
                .manifest_store
                .windows(encoded.len())
                .any(|window| window == encoded)
        );
        assert_eq!(
            assertions["c2pa.actions.v2"]["actions"][0]["action"],
            "c2pa.created"
        );
    }

    #[test]
    fn created_flag_on_a_soft_binding_is_kept() {
        let mut definition: serde_json::Value = serde_json::from_str(&with_soft_binding(
            r#"{"alg": "com.adobe.trustmark.Q", "blocks": [{"scope": {}, "value": "AAECAwQFBgc="}]}"#,
        ))
        .unwrap();
        definition["assertions"][1]["created"] = serde_json::Value::Bool(true);

        let signed = sign(dev().as_ref(), PNG, "image/png", &definition.to_string()).unwrap();

        let report: serde_json::Value = serde_json::from_str(&signed.report_json).unwrap();
        let created = report["manifests"][0]["claim.v2"]["created_assertions"]
            .as_array()
            .unwrap();
        assert!(
            created
                .iter()
                .any(|a| a["url"] == "self#jumbf=c2pa.assertions/c2pa.soft-binding")
        );
    }

    #[test]
    fn malformed_soft_binding_is_rejected() {
        for data in [
            r#"{"blocks": [{"scope": {}, "value": "AAE="}]}"#,
            r#"{"alg": "x", "blocks": []}"#,
            r#"{"alg": "x", "blocks": [{"scope": {}, "value": "not base64!"}]}"#,
            r#"{"alg": "x", "blocks": [{"scope": {}, "value": [0, 1]}]}"#,
            r#"{"alg": "x", "blocks": [{"scope": {"region": {}}, "value": "AAE="}]}"#,
        ] {
            let status = sign(dev().as_ref(), JPEG, "image/jpeg", &with_soft_binding(data))
                .err()
                .unwrap();
            assert_eq!(status.code(), tonic::Code::InvalidArgument, "{data}");
        }
    }

    #[test]
    fn created_action_without_source_type_is_rejected() {
        let definition = r#"{"assertions": [{"label": "c2pa.actions", "data": {"actions": [
            {"action": "c2pa.created"}]}}]}"#;
        let status = sign(dev().as_ref(), JPEG, "image/jpeg", definition)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }

    #[test]
    fn unparseable_asset_is_rejected() {
        let status = sign(dev().as_ref(), b"not a jpeg", "image/jpeg", DEFINITION)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }

    #[test]
    fn unsupported_format_is_rejected() {
        let status = sign(dev().as_ref(), b"plain text", "text/plain", DEFINITION)
            .err()
            .unwrap();
        assert_eq!(status.code(), tonic::Code::InvalidArgument);
    }
}
