# Development signer — NON-CONFORMANT

`es256.key.pem` is a private key committed to a repository on purpose. It is **public**,
so a signature made with it proves nothing about who signed.

- It exists so the local stack, the tests and CI can produce and validate real C2PA
  manifests without a KMS key or a certificate from a C2PA-listed CA.
- `es256.chain.pem` is the signing certificate followed by a private root made only for
  it. The root is not on the C2PA Trust List, so every verifier reports the signer as
  untrusted or "Unrecognized". That is the intended result.
- The sidecar loads it only when `SIGNUNDER_DEV_SIGNER_DIR` points here, and uses it only
  for requests whose `key_ref` is `dev:local`. `deploy/docker-compose.yml` sets that
  variable; no production configuration may.
- Never use it for content that leaves local development, and never add its root to a
  trust list.

`./generate.sh` makes a fresh pair (needs OpenSSL 3). The root's key is thrown away each
time. See ADR 0003.
