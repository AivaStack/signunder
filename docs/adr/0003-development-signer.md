# ADR 0003 — Development signer: a committed, public, non-conformant key

- **Status:** Accepted
- **Date:** 13 October 2026
- **Deciders:** Eoin

## Context

Phase 0 needs the sign sidecar to produce and validate real C2PA manifests locally, in tests and in CI, before there is a KMS key or a certificate from a C2PA-listed CA (SIGNUNDER.md §6, "local compose with software signer"). c2pa-rs is pinned at 0.91.2. That version offers two ways to sign without real credentials:

- `EphemeralSigner`, which generates a new Ed25519 chain on every call. The certificate changes on each start, and it is not one of the two algorithms the sidecar contract names (`es256`, `ps256`).
- `create_signer::from_keys`, which takes a PEM chain and a PEM private key. The crate's own test certificates are in its git repository but not in the published crate.

## Options considered

1. **Commit a key and chain made for this repo.** One stable certificate for tests, CI, Docker and samples. A private key sits in a repository that is meant to be opened, and secret scanners will flag it.
2. **Generate on each machine and gitignore it.** Nothing key-shaped is committed, but CI and Docker each need a generation step and every machine signs with a different certificate.
3. **`EphemeralSigner` at start-up.** No files at all, but Ed25519 only and a new certificate on every start.

## Decision

Option 1.

- `src/sign-sidecar/dev-signer/` holds `es256.key.pem` (P-256) and `es256.chain.pem` (the signing certificate, then a private root made only for it), with the `generate.sh` that made them. The root's key is discarded at generation. Subjects carry `O=SignUnder DEV - NON-CONFORMANT`.
- The sidecar can sign with the key file directly, for requests with `key_ref = "dev:local"` and `algorithm = "es256"`, but only when `SIGNUNDER_DEV_SIGNER_DIR` is set. The offline tests use this. No production configuration may set it.
- **Amended 13 October 2026 (ADR 0004).** The local stack no longer mounts the key into the sidecar. `generate.sh` also writes `local-kms-seed.yaml`, the same key as a seed for the local-kms emulator; `deploy/docker-compose.yml` gives that to the emulator and the stack signs through the sidecar's KMS path. The API container mounts only the certificate chain. The sidecar image still contains no key.
- The file signer goes through `create_signer::from_keys`, so it and the KMS signer both reach c2pa-rs as a `Signer`.
- The development stack signs without an RFC 3161 timestamp unless `SIGNUNDER_TSA_URL` is set, so tests and `make up` need no outside service. §4.7's "timestamp on every manifest" stands for production.

## Consequences

- The key is public. A signature from it proves nothing about the signer, and the root must never be added to a trust list. Verifiers report `signingCredential.untrusted`; c2pa-rs still reports the manifest as `Valid` when the claim signature and hard binding check out, and the tests assert exactly that.
- Secret scanning on the public repository will need an allow rule for `es256.key.pem`.
- Samples in `docs/conformance/samples/` are signed with this certificate. They show a well-formed manifest and a working hard binding, not conformant output, and need regenerating under a real certificate.
- The certificates expire on 10 October 2036. `generate.sh` replaces them.

## Open

- Whether the conformance application accepts samples signed by a non-listed certificate, or needs a certificate first. Not checked against the program documents.
