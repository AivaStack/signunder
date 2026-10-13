# Sample signed files

Made on 13 October 2026 by `make sample` (`tests/conformance/generate-samples.sh`): each
fixture image was posted to the API's `POST /v1/sign` on the local stack, signed by the
sidecar with c2pa-rs 0.91.2, and the result validated by the sidecar's `Validate` RPC.
Timestamps are from `http://timestamp.digicert.com`.

| File | Report | Result |
|---|---|---|
| `sample.jpg` | `sample.jpg.crjson` | Valid: claim signature, timestamp and data hash all validate |
| `sample.png` | `sample.png.crjson` | Valid, as above |
| `sample.tampered.jpg` | `sample.tampered.jpg.crjson` | Invalid: `assertion.dataHash.mismatch`. It is `sample.jpg` with one byte of image data flipped |

Read these before using them as evidence:

- **The signer is the development signer** (`src/sign-sidecar/dev-signer/`, ADR 0003). Its
  key is public and its root is not on the C2PA Trust List, so every report carries
  `signingCredential.untrusted` and a verifier will show the signer as unrecognised. The
  files show that the manifest is well formed and the hard binding works; they are not
  conformant output. Samples for the application itself need regenerating once a real
  certificate exists.
- **`timeStamp.untrusted` is informational.** The timestamp validates, but no timestamp
  trust anchors are configured in the sidecar yet.
- **The images are synthetic gradients, not model output**, although the manifest
  declares `trainedAlgorithmicMedia` as the product will.
- Reports contain a `validationTime` and the manifests a random label, so regenerating
  changes every file.
