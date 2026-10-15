# Cloud KMS claim signing key, version 1 — evidence

Downloaded 15 October 2026 with `gcloud kms keys versions describe --attestation-file`,
`get-certificate-chain` and `get-public-key`.

| | |
|---|---|
| Key version | `projects/vaulted-journal-511706-v2/locations/europe-west3/keyRings/signunder/cryptoKeys/claim-signing/cryptoKeyVersions/1` |
| Protection level | HSM |
| Algorithm | `EC_SIGN_P256_SHA256` |
| Generated | 2026-10-15T06:50:58Z |
| Attestation format | `CAVIUM_V2_COMPRESSED` |

| File | What it is |
|---|---|
| `attestation.dat` | The HSM's signed statement about the key version |
| `chain-cavium.pem` | The HSM manufacturer's (Cavium/Marvell) certificate chain |
| `chain-google-card.pem`, `chain-google-partition.pem` | Google's chains, to "Hawksbill Root v1 prod" |
| `public-key.pem` | The key version's public key |

Not yet verified with Google's attestation verification script; that is the step that
turns these files into evidence that the key was generated in an HSM and cannot be
extracted (Cloud KMS documentation, "Verifying attestations", read 15 Oct 2026).

Checked on 15 October 2026, acting as the sidecar's service account `signunder-signer`:
reading the public key and signing a digest work (openssl verifies the signature with
this public key); listing the key ring's keys and destroying the key version are refused
with `PERMISSION_DENIED`.
