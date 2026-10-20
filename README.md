# SignUnder

SignUnder signs AI-generated images under the name of the company that publishes them, so the obligation to mark AI output (EU AI Act Article 50, California SB 942, platform labelling rules) is met in that company's own name. Each output gets:

- **C2PA Content Credentials**, signed with a key held in a cloud HSM, saying the image was made by a generative model, and optionally a CAWG identity assertion naming the company and its verified domain;
- **an invisible watermark** (TrustMark) bound into the manifest, so the record survives when platforms strip metadata;
- **a perceptual fingerprint** (PDQ), as a last resort when both are gone;
- **a registry record**, so anyone can check on the company's public verify page whether an image is one of its outputs.

A rules engine says which marking duties apply to a given output in a given place, with each rule citing its primary source and the date it was read.

The hosted service is at [signunder.com](https://signunder.com); the API is documented at [signunder.com/docs](https://signunder.com/docs). This repository is the whole of it except the website.

## What's here

| Path | What it is |
|---|---|
| `src/Signunder.Api` | The API (.NET 10): signing, verification, the registry, accounts, the public verify routes |
| `src/Signunder.Core`, `src/Signunder.Rules` | The domain model and the rules engine with its rule packs |
| `src/Signunder.Sdk` | The .NET SDK |
| `src/signunder-py` | The Python SDK, on PyPI as [`signunder`](https://pypi.org/project/signunder/) |
| `src/sign-sidecar` | The signer (Rust, c2pa-rs) behind gRPC, with Cloud KMS and the identity CA tools |
| `src/wm-worker` | Watermarking and fingerprinting (Python, TrustMark and PDQ) behind gRPC |
| `proto/` | The contracts between the API and the two workers |
| `content/` | The reference pages and blog posts published on signunder.com |
| `docs/` | [Architecture](docs/ARCHITECTURE.md), [decision records](docs/adr/), conformance evidence |
| `deploy/` | Docker Compose for the local stack; Fly.io configurations for staging |

## Running it locally

You need .NET 10, Rust, Python with [uv](https://docs.astral.sh/uv/), protobuf and Docker (`Brewfile` lists them for macOS).

```
make build   # .NET and Rust
make test    # all tests; the API's start Postgres in Docker
make up      # the whole stack in Docker, signing with the public development key
make sample  # with the stack up: sign the fixtures into docs/conformance/samples
```

The local stack signs with a development key and certificate committed for that purpose; Content Credentials viewers show its output as unrecognised. Signing for real needs a key in a KMS and a certificate from a C2PA-recognised certificate authority.

## Licence

Apache-2.0: see [LICENSE](LICENSE). Security reports: [SECURITY.md](SECURITY.md).
