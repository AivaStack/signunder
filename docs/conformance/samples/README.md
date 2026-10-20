# Sample signed files

Made on 14 October 2026 by `make sample` (`tests/conformance/generate-samples.sh`): each
fixture image was posted to the API's `POST /v1/sign` on the local stack, watermarked
(TrustMark), signed by the sidecar through the KMS code path (c2pa-rs 0.91.2, local-kms
emulator), and the result validated by the sidecar's `Validate` RPC. Timestamps are
from `http://timestamp.digicert.com`.

| File | Report | Result |
|---|---|---|
| `sample.jpg` | `sample.jpg.crjson` | Valid: claim signature, timestamp and data hash all validate |
| `sample.png` | `sample.png.crjson` | Valid, as above |
| `sample.webp` | `sample.webp.crjson` | Valid, as above |
| `sample.tampered.jpg` | `sample.tampered.jpg.crjson` | Invalid: `assertion.dataHash.mismatch`. It is `sample.jpg` with one byte of image data flipped |

Each manifest declares `specVersion` 2.4 in `claim_generator_info`, files its actions
(`c2pa.created` with `trainedAlgorithmicMedia`, then `c2pa.watermarked.bound`, with
`allActionsIncluded: true`) and its `c2pa.soft-binding` assertion as created assertions.

**Checked against the program's own rubric.** `make conformance` runs the C2PA
Conformance Program's asset rubric (program v0.2, spec 2.4, Asset Rubrics 1.0) over the
three valid samples: 31 of 31 checks pass on each (14 October 2026).

## With the company's name (CAWG identity)

Made on 20 October 2026 on **staging**, not the local stack: each fixture was posted to
`https://signunder-staging-api.fly.dev/v1/sign` with the key of VersionR, the test company
whose name and domain (versionr.ai) were approved on 20 October (ADR 0014), and validated
by the local sidecar's `Validate` RPC. So these went through the deployed system: the
Cloud KMS HSM claim-signing key, the `identity-signing` HSM key and the DigiCert timestamp.

| File | Report | Result |
|---|---|---|
| `identity.jpg` | `identity.jpg.crjson` | Valid, as above, plus a `cawg.identity` assertion (role `cawg.publisher`, O=VersionR, CN=versionr.ai): `cawg.identity.well-formed`, `cawg.x509.signature.validated` |
| `identity.png` | `identity.png.crjson` | Valid, as `identity.jpg` |
| `identity.webp` | `identity.webp.crjson` | Valid, as `identity.jpg` |

The rubric passes 31 of 31 checks on each (20 October 2026, `c2pa-org/conformance-public`
at `74ade44500ef`, spec 2.4), as on the three samples without the assertion: adding it
breaks nothing the program checks. Besides the notes below, each report carries
`cawg.x509.credential.untrusted`, because no identity trust list includes the SignUnder
Identity CA, and the claim certificate is staging's test certificate ("SignUnder TEST -
NOT CONFORMANT"), so `signingCredential.untrusted` remains.

Read these before using them as evidence:

- **The signer is the development key** (`src/sign-sidecar/dev-signer/`, ADR 0003),
  held by the local-kms emulator. Its root is not on the C2PA Trust List, so every report
  carries `signingCredential.untrusted`. Samples for the application should come from
  the deployed system with its real KMS key; the identity samples above do, but still
  with staging's test certificate.
- **`timeStamp.untrusted` is informational.** The timestamp validates, but no timestamp
  trust anchors are configured in the sidecar yet.
- **The images are synthetic gradients, not model output**, although the manifest
  declares `trainedAlgorithmicMedia` as the product will.
- Reports contain a `validationTime` and the manifests a random label, so regenerating
  changes every file.
