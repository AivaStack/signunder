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

Read these before using them as evidence:

- **The signer is the development key** (`src/sign-sidecar/dev-signer/`, ADR 0003),
  held by the local-kms emulator. Its root is not on the C2PA Trust List, so every report
  carries `signingCredential.untrusted`. Samples for the application should come from
  the deployed system with its real KMS key.
- **`timeStamp.untrusted` is informational.** The timestamp validates, but no timestamp
  trust anchors are configured in the sidecar yet.
- **The images are synthetic gradients, not model output**, although the manifest
  declares `trainedAlgorithmicMedia` as the product will.
- Reports contain a `validationTime` and the manifests a random label, so regenerating
  changes every file.
