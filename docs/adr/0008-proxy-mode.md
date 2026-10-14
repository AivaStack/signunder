# ADR 0008 — Proxy mode

- **Status:** Accepted
- **Date:** 14 October 2026
- **Deciders:** Eoin

## Context

SIGNUNDER.md §4.3 describes a proxy that sits in front of fal and Replicate and signs their outputs in flight. Their documentation (read 14 Oct 2026) changed the picture:

- Neither returns image bytes. fal returns `images[].url` on `*.fal.media`, subject to expiry; Replicate returns URLs on `replicate.delivery`, deleted after an hour and fetched with the customer's token.
- fal works through a queue (`queue.fal.run`: submit, poll `status_url`, fetch `response_url`); Replicate creates a prediction, optionally waiting up to 60 s with `Prefer: wait`, otherwise it is polled at `urls.get`.
- `Authorization` already carries the provider's key (`Key …` for fal, `Bearer …` for Replicate).
- fal's JavaScript client has a `proxyUrl` mode; Replicate's has `baseUrl`. fal's Python client documents no host override. "Zero code change" is not available.

## Decision

- **The customer's provider key passes through.** SignUnder never stores a fal or Replicate key. The SignUnder key travels in `X-SignUnder-Key`, which the API now accepts on every endpoint, and needs the `sign` scope. It is not forwarded upstream.
- **Routes mirror the providers' paths.** `/proxy/fal/queue/{**path}` maps to `queue.fal.run/{path}` (GET, POST, PUT, DELETE). `/proxy/replicate/v1/…` covers creating a prediction (both forms), getting one and cancelling one. Forwarded headers are `Authorization` (both) and `Prefer` (Replicate).
- **Follow-up URLs are rewritten** in every JSON response so status, result and cancel calls come back through the proxy.
- **Signing happens when a finished result is fetched.** For fal, a GET of `{model}/requests/{id}` whose body has `images[]` or `image`; for Replicate, a prediction with `status = succeeded`. Each image is downloaded from the provider's media hosts only: an allowlist, HTTPS, no redirects, 32 MB at most. It then goes through the same pipeline as `/v1/sign` (watermark, fingerprint, sign, validate, record with `provider` and `model_name`). The pipeline moved into `ImageSigner` so both share it.
- **The response points at SignUnder's copy.** The image URL becomes `/media/{record id}`; fal results gain `signunder_record_id`. `/media/{id}` needs no key, like the providers' own result URLs.
- **Fail closed.** An image that cannot be signed (host not allowed, plain HTTP, unsupported type, too large, a signing error) fails the call with 502. Non-image outputs pass through.
- **The model is remembered at submit.** fal's result URLs drop the model's last path segment (a request to `fal-ai/flux/schnell` is fetched at `fal-ai/flux/requests/{id}`, seen on a live call 14 Oct 2026), so the submitted model is kept in `proxy_request` and used for the record's `model_name`.
- **Each result is signed once.** The rewritten body is kept in `proxy_result` (tenant, provider, upstream id) and returned on later fetches.
- **Signed copies are kept in S3-compatible storage**: Supabase Storage in production, `adobe/s3mock` in the local stack. MinIO, which §4.11 named, no longer publishes images on Docker Hub. Copies are meant to expire after 30 days (§4.10).
- **WebP is supported** end to end, because Replicate's FLUX models return it by default.

## Consequences

- SignUnder now holds signed media for a time, which §4.1's "hashes, not media" did not allow. The registry still holds only hashes; the copy is transient.
- Customers change a base URL and add one header.
- Signing adds its time to the result fetch (about 0.9 s for one image in the local run).
- If one of several images fails, the earlier ones stay signed and recorded although the call fails. Two simultaneous first fetches can both sign; one result is kept and the other's records remain.
- fal: tested against live fal-ai/flux/schnell on 14 Oct 2026 (one generation direct, one through the proxy); the tests' fal stand-in returns the recorded bodies. Replicate: tested only against a stand-in shaped from its documentation.

## Open

- A run against a real Replicate account.
- The 30-day expiry: nothing enforces it yet. It needs a lifecycle rule or a sweep on Supabase Storage (UNVERIFIED whether Supabase supports S3 lifecycle rules).
- fal's `proxyUrl` mode (`x-fal-target-url`), fal's synchronous `fal.run`, webhooks, and Replicate deployments and streaming.
- Moving signing behind the job queue so the provider call returns quickly.
- Whether `/media/{id}` should need a signature or expire sooner.
