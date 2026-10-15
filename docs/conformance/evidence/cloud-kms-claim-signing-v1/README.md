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

## Verification, 15 October 2026

With Google's scripts from `GoogleCloudPlatform/python-docs-samples`, `kms/attestations`
(commit `08e010a`, 14 Oct 2026), and their pinned packages (`cryptography` 50.0.1, `pem` 23.1.0):

| Check | Result |
|---|---|
| `verify_attestation.py` with `chain-cavium.pem` | Signature verified |
| `verify_attestation.py` with `chain-google-partition.pem` | Signature verified |
| `verify_attestation.py` with `chain-google-card.pem` | Fails, as expected: the partition signs attestations, not the card |
| `verify_attestation_chains.py` | **Not run to the end.** It downloads the HSM maker's root certificate from marvell.com, which refuses scripted downloads (HTTP 403) |

Checked separately, because of the gap:

| Check | Result |
|---|---|
| Google's root "Hawksbill Root v1 prod", from `gstatic.com/cloudhsm/roots/global_1498867200.pem` | Same certificate as the copy in Google's script (SHA-256 `46:B5:FD:35:…:E0:95`) |
| The root issued Google's card and partition certificates | openssl: OK, both |
| The Cavium card certificate issued the Cavium partition certificate | Signature OK, checked as Google's script does |
| Google's and Cavium's certificates are for the same card key and the same partition key | Yes, both |
| The attestation is about this key | The key version's public point (`04‖X‖Y`, from `public-key.pem`) appears in the decompressed attestation |

Not yet checked:
- That Marvell's root (`localca.liquidsecurity.cavium.com`) issued the Cavium card certificate.
- The key attributes inside the attestation, such as non-extractable.

These are the remaining steps that make the files evidence that the key was generated in a
Marvell HSM and cannot be extracted (Cloud KMS documentation, "Verifying attestations",
read 15 Oct 2026).

Checked on 15 October 2026, acting as the sidecar's service account `signunder-signer`:
reading the public key and signing a digest work (openssl verifies the signature with
this public key); listing the key ring's keys and destroying the key version are refused
with `PERMISSION_DENIED`.
