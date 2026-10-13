# Development signer — NON-CONFORMANT

`es256.key.pem` is a private key committed to a repository on purpose. It is **public**,
so a signature made with it proves nothing about who signed.

- It exists so the local stack, the tests and CI can produce and validate real C2PA
  manifests without a KMS key or a certificate from a C2PA-listed CA.
- `es256.chain.pem` is the signing certificate followed by a private root made only for
  it. The root is not on the C2PA Trust List, so every verifier reports the signer as
  untrusted or "Unrecognized". That is the intended result.
- `local-kms-seed.yaml` is the same key as a seed for the local-kms emulator.
  `deploy/docker-compose.yml` gives it to the emulator, so the local stack signs through
  the sidecar's KMS path with this key, and `make test-kms` does the same in tests.
- The sidecar can also sign with the key file directly, for requests whose `key_ref` is
  `dev:local`, but only when `SIGNUNDER_DEV_SIGNER_DIR` points here. The offline tests
  use that; no production configuration may set it.
- Never use it for content that leaves local development, and never add its root to a
  trust list.

`./generate.sh` makes a fresh pair (needs OpenSSL 3). The root's key is thrown away each
time. See ADR 0003.
