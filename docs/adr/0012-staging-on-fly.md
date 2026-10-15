# ADR 0012 — Staging on Fly.io

- **Status:** Accepted
- **Date:** 15 October 2026
- **Deciders:** Eoin

## Context

ADR 0002 put the API, sign sidecar and watermark worker on Fly.io in Frankfurt, and ADR 0011 put the signing key in Google Cloud KMS. This records how staging was deployed on 15 October 2026, and what the first deployment taught. Eoin asked for costs to stay small: Fly.io's pricing page lists no spending cap (read 15 Oct 2026).

## Decision

- **Three apps in the `eoin-butler` (personal) organisation, region `fra`, one machine each** (`--ha=false`):
  - `signunder-staging-api`: 512 MB, public;
  - `signunder-staging-sidecar`: 512 MB, private;
  - `signunder-staging-wm`: 2 GB, private. About 920 MB is in use after marking a 1920×1920 image.

  Configurations are in `deploy/fly/*-staging.toml`; deploy from the repository root.
- **Suspend when idle, start on demand.** Fly.io bills a suspended machine as stopped (storage only) and resumes it in a few hundred milliseconds (suspend/resume reference, 15 Oct 2026). Suspend needs 2 GB of memory or less.
  - Prices from Fly.io's pricing page: always on, the three would cost about $23 a month; idle, under $1.
  - Measured: Fly.io suspended all three after about 5 idle minutes, and the first sign afterwards took 3.4 s.
- **Addresses.** The API has a shared IPv4 (free) and a public IPv6; there are no dedicated IPv4s and no volumes. The sidecar and worker have only private Flycast addresses (`*.flycast`), through which Fly.io's proxy starts them on demand and passes their TLS through untouched.
- **Secrets as files.** Internal TLS certificates and keys (ADR 0010, a staging set generated outside the repository with the Flycast names), the signer's certificate chain and the database connection are Fly secrets. Files are mounted with `[[files]] secret_name`, which requires the secret to be stored base64-encoded; Fly.io gives the files to the image's user.
- **Keyless signing** (ADR 0011). Fly.io writes a machine identity token to `/.fly/oidc_token`, but only when `AWS_ROLE_ARN` is set, so the sidecar sets a dummy one; nothing calls AWS.
  - The token: issuer `https://oidc.fly.io/eoin-butler`, audience `sts.amazonaws.com`, subject `org:app:machine`, 10-minute lifetime, readable by every user.
  - `deploy/fly/gcp-wif-staging.json`, which holds no secret, exchanges it at the workload identity pool `fly-io`, provider `fly-eoin-butler`. The provider accepts only that issuer and audience, from apps whose name starts `signunder-`.
  - Only `signunder-staging-sidecar` may act as `signunder-signer`.
- **Database.** Supabase, through the session pooler (`aws-1-eu-central-1.pooler.supabase.com:5432`), as role `signunder_api`, which owns schema `registry` and can create nothing in `public` (ADR 0005). On staging the API applies migrations and seeds one tenant at start-up. Its API key is a Fly secret, also kept in the local `.env` as `SIGNUNDER_STAGING_API_KEY`.
- **Health check** on `/healthz`, sending `Host: signunder-staging-api.fly.dev`, so `AllowedHosts` can stay limited to the public name.
- **Staging signs with a certificate from a throwaway test CA** ("SignUnder TEST - NOT CONFORMANT") issued for the HSM key's request from `kms-csr`, until the conformance listing. Timestamps come from DigiCert's public TSA.

## What deploying changed in the code

- **The watermark worker opens its port before loading the model.** It used to load first, which took about 20 s on a shared CPU, longer than Fly.io's proxy waits for a woken machine (about 8 s), so the first call failed. Calls made while the model loads now wait for it.
- **The API's internal gRPC connections tolerate suspension.**
  - Idle connections close after 15 s.
  - HTTP/2 pings, sent only during a call, detect a dead connection in seconds.
  - A call that failed in the transport (a network exception under Internal or Unavailable) is retried on a new connection, which wakes the service.
- **Signed copies are deleted after 30 days by the API** (`MediaSweep`), because Supabase Storage lifecycle rules only expire previous versions of objects.

## Consequences

- A staging request that wakes suspended services takes a few seconds. One arriving moments after a manual suspend took about 21 s while dead connections were detected; that cannot happen with automatic suspension, which waits minutes.
- The sweep runs only while the API is awake; it catches up on waking.
- Fly.io's public edge accepts TLS 1.3 and 1.2 for the API (checked 15 Oct 2026). The program's TLS 1.3 requirement covers channels between SignUnder's own subsystems, which use mutual TLS (ADR 0010).
- The dummy `AWS_ROLE_ARN` depends on undocumented Fly.io behaviour, and could break if Fly.io changes how it writes the token.

## Open

- Production apps, certificates and tenant set-up; a release step for migrations rather than start-up.
- Whether the token in `/.fly/oidc_token` is refreshed for a machine that stays awake longer than 10 minutes.
- An S3 access key for the `signed-media` bucket (dashboard only), so proxy mode can keep signed copies on staging.
- A spending alert, if Fly.io's billing settings offer one.
