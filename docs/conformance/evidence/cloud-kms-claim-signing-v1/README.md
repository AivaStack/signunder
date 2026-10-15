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
| `verify_attestation_chains.py` | **The attestation has been verified**: both certificate chains built, and both partitions' signatures check. Run with Marvell's root read from a copy of `liquid_security_certificate.zip` downloaded in a browser (marvell.com refuses scripted downloads with HTTP 403); the script's download function was replaced by one reading that file, and nothing else was changed. Control: with one byte of the attestation altered, the same run fails |

Marvell's root in that zip: `CN=localca.liquidsecurity.cavium.com`, SHA-256 fingerprint
`97:57:57:F0:…:CC:28` (zip SHA-256 `ea68f351…ba1858`). It expired on 16 Nov 2025. Google's
script checks signatures, not validity dates, and Marvell still publishes this file as the root.

Also checked on their own:

| Check | Result |
|---|---|
| Google's root "Hawksbill Root v1 prod", from `gstatic.com/cloudhsm/roots/global_1498867200.pem` | Same certificate as the copy in Google's script (SHA-256 `46:B5:FD:35:…:E0:95`) |
| The root issued Google's card and partition certificates | openssl: OK, both |
| The Cavium card certificate issued the Cavium partition certificate | Signature OK, checked as Google's script does |
| Google's and Cavium's certificates are for the same card key and the same partition key | Yes, both |
| The attestation is about this key | The key version's public point (`04‖X‖Y`, from `public-key.pem`) appears in the decompressed attestation |

Together these show that an HSM certified by its maker (Marvell, formerly Cavium) and by Google
signed a statement about this key version. Not checked: the key attributes inside the
attestation, such as non-extractable; Google's scripts do not parse them (Cloud KMS
documentation, "Verifying attestations", read 15 Oct 2026).

Checked on 15 October 2026, acting as the sidecar's service account `signunder-signer`:
reading the public key and signing a digest work (openssl verifies the signature with
this public key); listing the key ring's keys and destroying the key version are refused
with `PERMISSION_DENIED`.
