# ADR 0004 — KMS signer: one `Sign` call per signature, local-kms as the stand-in

- **Status:** Accepted; amended by ADR 0011 (15 Oct 2026): production keys are in Google Cloud KMS, and the AWS SDK path described here is kept only for the local-kms emulator.
- **Date:** 13 October 2026
- **Deciders:** Eoin

## Context

SIGNUNDER.md §4.1 and §4.5 require that a signing key never exists in software: it lives in AWS KMS and signing is a KMS `Sign` call. The sidecar contract (`sign.proto`) already carries a `key_ref`, an algorithm and a certificate chain. ADR 0003 gave the sidecar a file-based development signer; this record covers the KMS path. c2pa-rs is pinned at 0.91.2.

No AWS account, credentials or CLI were available when this was built, so nothing here has run against real AWS KMS.

## Decision

- **Routing.** A `key_ref` beginning `arn:aws:kms:` selects the KMS signer; `dev:local` selects the file signer of ADR 0003; anything else is refused. With a KMS key the caller supplies `cert_chain_pem`, signing certificate first.
- **Signing.** Each signature is one KMS `Sign` call with `ECDSA_SHA_256`. The sidecar hashes the bytes c2pa-rs hands it with SHA-256 and sends `MessageType = DIGEST`, because KMS accepts at most 4096 bytes of raw message and a claim can be larger. KMS returns a DER signature; c2pa-rs converts it to the fixed-width form COSE requires.
- **Integration with c2pa-rs.** The signer is c2pa-rs's own `CallbackSigner` with a closure that calls KMS, not a hand-written `Signer` implementation. Signing runs on a blocking thread and the closure blocks on the tokio runtime for the KMS call.
- **Client.** The official AWS SDK for Rust (`aws-config`, `aws-sdk-kms`), with region, credentials and endpoint taken from the environment. Hand-rolled request signing was not considered worth owning.
- **Local and CI stand-in.** The `nsmithuk/local-kms` emulator, seeded with the development key (ADR 0003), runs in `deploy/docker-compose.yml`; the sidecar reaches it through `AWS_ENDPOINT_URL`. `make test-kms` runs the emulator-backed tests and CI runs it. `make test` stays offline.
- **Certificate check (added 13 October 2026).** Before signing, the sidecar compares the public key in the signing certificate with KMS `GetPublicKey` for the key, cached per key ARN for the life of the process. A certificate for another key is refused as a failed precondition and a missing key as not found, before any `Sign` call.
- **Validation.** As with every signer, the signed asset is validated before it leaves the sidecar.

## Consequences

- The sidecar gains the AWS SDK and its dependency tree, and its image takes longer to build. c2pa stays at exactly 0.91.2.
- Every sign costs one KMS round trip. The effect on the 100 signs a second target (§5, Load) is unmeasured.
- Only ES256 is implemented. `ps256` with a KMS key returns "unimplemented": there was no RSA key and certificate to test against.
- A `GetPublicKey` failure other than a missing key is reported as unavailable. A failure of the `Sign` call itself still surfaces as an internal error carrying the KMS message.
- The emulator proves the request shape and the signature handling. It proves nothing about IAM key policy, real error responses or latency.

## Open

- A run against real AWS KMS in a test account, and the §5 "Key path" suite (key policy denies export and use outside the signer role).
- PS256.
- Whether the per-key public key cache needs bounding or expiry once there are many tenants.
