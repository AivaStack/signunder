# SignUnder

Hosted service that gets a company signing AI-generated content under its own name (C2PA conformance, signing, watermarking, verification, rule packs). The folder is still called `Cereal`, the old placeholder name.

## Documents

- `SIGNUNDER.md` is the product, architecture and build plan. Its section 7 is the decision log and section 8 the open questions.
- `docs/adr/` holds one record per technical decision, numbered `NNNN-short-title.md`.
- When a decision makes either of them, or this file, out of date, propose the specific change with a Yes/No question at the next checkpoint. Do not edit them silently and do not wait to be asked.
- `SIGNUNDER.md` marks anything not read from a primary source as UNVERIFIED or *from memory*. Keep to that.
- `docs/marketing/SEO-GEO-PLAN.md` is the search and AI-search marketing plan; `docs/marketing/CHECKLIST.md` is its work list. Blog and reference content lives in `content/` as Markdown and is pulled into `signunder-web` at build. Law content follows the same rule: cite the primary source and the date read, or mark it UNVERIFIED.

## Layout

- `src/Signunder.Api`, `Signunder.Core`, `Signunder.Rules`, `Signunder.Sdk`: .NET 10 (version pinned in `global.json`, shared settings in `Directory.Build.props`, warnings are errors).
- `src/sign-sidecar`: Rust, wraps c2pa-rs, talks gRPC to the API.
- `src/wm-worker`: Python, managed with uv.
- `proto/signunder/sign/v1/sign.proto`: the API-to-sidecar contract.
- `tests/`: xUnit projects for Core, Rules and the API (`Signunder.Api.Tests` hosts the API against Postgres in a container with a stand-in sidecar); `tests/conformance` holds the sample generator; the other folders are placeholders.
- `deploy/docker-compose.yml`: local stack.
- `content/`: blog posts and reference pages (law, platforms, labels, guides, compare) as Markdown, with templates in `content/_templates/`. Published by `signunder-web`, never rendered from here.
- The web front end (Next.js, shadcn/ui, Zippay template) lives in a separate private repo, `signunder-web`. Never copy template code into this repo: it is licensed, and this repo is meant to be opened.

## Commands

```
make build   # dotnet build + cargo build
make test    # dotnet test + cargo test (the API tests start Postgres in Docker)
make test-kms  # sidecar tests that sign through the local-kms emulator (needs Docker)
make fmt     # dotnet format + cargo fmt
make up      # build and start the local stack in Docker
make down
make sample  # with the stack up: sign the fixtures into docs/conformance/samples
```

Toolchains are listed in the `Brewfile`. Install the missing formulae one by one (`brew install rustup protobuf`, then `rustup default stable`) rather than running `brew bundle`, which installs the `dotnet-sdk` cask over a dotnet that came from elsewhere. Homebrew's rustup is keg-only: `cargo` needs `/opt/homebrew/opt/rustup/bin` on PATH.

## Hosting

Vercel for the web front end, Supabase (EU) for Postgres, auth, storage and the queue, Fly.io (Frankfurt) for the API, sidecar and worker containers, AWS KMS for signing keys. See ADR 0002.
