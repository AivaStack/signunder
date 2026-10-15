# ADR 0011 — Google Cloud KMS instead of AWS KMS

- **Status:** Accepted
- **Date:** 15 October 2026
- **Deciders:** Eoin
- **Amends:** ADR 0002 (AWS KMS for signing keys), ADR 0004 (the KMS signer)

## Context

ADR 0002 kept AWS KMS for signing keys, and ADR 0004 built the sidecar's signer on the AWS SDK, tested against the local-kms emulator. Before any AWS resource was created, Eoin decided SignUnder should not use AWS at all: several existing AWS accounts with different cards attached make a further one confusing to run.

Read on 15 Oct 2026:

- **Same signing model.** Google Cloud KMS supports `EC_SIGN_P256_SHA256` asymmetric keys. `asymmetricSign` takes a digest or the data and returns a DER-encoded signature; `getPublicKey` returns PEM ([docs](https://docs.cloud.google.com/kms/docs/create-validate-signatures)).
- **Cloud HSM is available in `europe-west3` (Frankfurt)** ([locations](https://docs.cloud.google.com/kms/docs/locations)).
- **Key attestations.** For HSM-protected key versions Google provides an attestation, signed by certificate chains from Google and the HSM manufacturer (Marvell), that proves the key was created in an HSM and is not extractable ([attestation](https://docs.cloud.google.com/kms/docs/attest-key)).
- **A Rust client.** Google publishes `google-cloud-kms-v1` (1.15.0, 24 Sep 2026) and `google-cloud-auth`, which supports workload identity federation from a file, URL or executable credential source.
- **The C2PA Generator Product Security Requirements v0.2** list Google Cloud KMS among acceptable key management environments.
- **Fly.io** lists Google Cloud among the providers its machine OpenID Connect tokens work with, but documents setup steps only for AWS ([Fly.io](https://docs.fly.io/reference/openid-connect)).

## Decision

- **Production claim signing keys live in Google Cloud KMS**, in a project used only for SignUnder, key ring in `europe-west3`, protection level **HSM**, algorithm `EC_SIGN_P256_SHA256`, purpose `ASYMMETRIC_SIGN`.
- **Access.** A service account that may only sign with and read the public key of that key (`roles/cloudkms.signerVerifier` and `roles/cloudkms.publicKeyViewer` on the key, not the project).
- **Fly.io machines reach it without stored keys**, through workload identity federation trusting Fly.io's OpenID Connect issuer for SignUnder's organisation and sidecar app. If that cannot be made to work, the fallback is a service-account key held as a Fly.io secret and rotated quarterly; using it would be recorded here.
- **No AWS account** is created or used for SignUnder.
- **The sidecar keeps two KMS backends** behind one interface (public key; sign a SHA-256 digest), chosen by the key reference:
  - `projects/…/cryptoKeyVersions/N` is Google Cloud KMS through `google-cloud-kms-v1`, with Application Default Credentials, built on first use and without its default aws-lc TLS provider;
  - `arn:aws:kms:…` is the AWS KMS API, kept only to talk to the local-kms emulator in the local stack and CI. Google has no Cloud KMS emulator.
- **`kms-csr`** shares that code and takes either kind of key reference (`--key`).
- **`make test-gcp`** signs through a real Cloud KMS key version and verifies the signature; `make test-kms` stays emulator-only.

## Consequences

- One fewer data processor: Google is already one (Workspace, Analytics); AWS no longer is.
- Cloud HSM key attestations may give the hardware-backed evidence that Assurance Level 2 asks for (Security Requirements v0.2, O.2 Level 2). Not yet checked against the program's exact wording.
- The production signing path is not exercised by the local stack or CI. It is tested by `make test-gcp` against a real key, which costs a few cents per run.
- Workload identity federation from Fly.io to Google Cloud is unproven until the first deploy.
- Price, from Google's Cloud Billing catalog on 15 Oct 2026 (the pricing page did not load): an active HSM ECDSA P-256 key version is **€2.20 a month** (for the first 2,000), and each HSM P-256 signature **€0.000013** (€0.13 per 10,000). A software key version would be €0.05 a month and €0.000003 per signature.
- **Created 15 Oct 2026:** `projects/vaulted-journal-511706-v2/locations/europe-west3/keyRings/signunder/cryptoKeys/claim-signing/cryptoKeyVersions/1` (HSM, `EC_SIGN_P256_SHA256`), and service account `signunder-signer` with `roles/cloudkms.signerVerifier` and `roles/cloudkms.publicKeyViewer` on that key only. Checked acting as that account: signing and reading the public key work and openssl verifies the signature; listing keys and destroying the version are refused. Eoin's account may act as the service account (`roles/iam.serviceAccountTokenCreator` on it) so tests run with its permissions. The HSM attestation and certificate chains are in `docs/conformance/evidence/cloud-kms-claim-signing-v1/`.

## Consequences found at the first deploy (15 Oct 2026)

- **Workload identity federation from Fly.io works** (details in ADR 0012). Pool `fly-io`, provider `fly-eoin-butler`, issuer `https://oidc.fly.io/eoin-butler`, audience `sts.amazonaws.com`, attribute condition `assertion.org_name == 'eoin-butler' && assertion.app_name.startsWith('signunder-')`.
- `signunder-signer` grants `roles/iam.workloadIdentityUser` only to the staging sidecar app's principal set.
- The machine reads Fly.io's token from `/.fly/oidc_token`, which Fly.io writes only when `AWS_ROLE_ARN` is set; the sidecar sets a dummy value.
- Checked: the sidecar on Fly.io signs through the HSM key, with no Google credential stored anywhere.

## Open

- Verify the HSM attestation with Google's verification script.
- Read the Level 2 attestation requirements against Cloud HSM attestations before offering Level 2.
- Rotation procedure for Cloud KMS key versions (a new version, a new certificate, the old version disabled, not destroyed).
