# OWASP Top 10 review of the SignUnder API

**14 October 2026.** Evidence for the C2PA Generator Product Security Requirements v0.2,
O.6 Level 1 ("vulnerability scanning and/or security review … for software dependencies
and API surfaces … at least … OWASP Top 10"). Reviewed against **OWASP Top 10:2025**
(category list from the project's source repository, `OWASP/Top10`, `2025/docs/en`, read
the same day).

## Scope and method

- **Surface:** the public HTTP API in `src/Signunder.Api` at commit after `5e7fbec`:
  - `/healthz` and `/readyz`;
  - `POST /v1/sign`, `POST /v1/verify` and `GET /v1/records/{id}`;
  - `GET /media/{id}`;
  - the proxy routes under `/proxy/fal/` and `/proxy/replicate/`.

  The API's calls to the sign sidecar, the watermark worker, AWS KMS, Postgres and object storage are covered where they matter to a category.
- **Dynamic scan:** OWASP ZAP 2.17.0, `zap-api-scan.py` against the local stack, driven by the API's OpenAPI description and authenticated with a development API key.
  - Before fixes: 117 checks passed and 2 warned (missing `X-Content-Type-Options` and `Cross-Origin-Resource-Policy`).
  - After fixes: **119 passed, 0 warnings, 0 failures**. Report: `evidence/zap-api-scan-2026-10-14.html` (and `.json`).
- **Code review:** each category below, against the source and the automated tests.
- **Not done:** a manual penetration test, or a scan of the production deployment (not yet deployed).

## Findings fixed in this review

| # | Finding | Category | Fix | Test |
|---|---|---|---|---|
| 1 | Internal error details reached callers: a failed sign returned text such as "Sign sidecar: Internal: KMS Sign with arn:aws:kms:…" | A10, A02 | The detail is logged with a reference; the caller gets "Signing failed. Quote reference …" | `HardeningTests.Internal_failures_are_reported_by_reference_without_details` |
| 2 | No `X-Content-Type-Options` or `Cross-Origin-Resource-Policy` headers (ZAP 10021, 90004) | A02 | `nosniff` on every response; `same-origin` except signed copies under `/media`, which are `cross-origin` because customers embed them | `HardeningTests.Responses_carry_the_security_headers`; ZAP rerun clean |
| 3 | Authenticated responses could be cached | A02 | `Cache-Control: no-store` on every authenticated response | as above |
| 4 | No rate limiting | A06 | Fixed window per API key, `RateLimit:PerKeyPerMinute` (default 300); per address for callers without a key; 429 with `Retry-After: 60` | `RateLimitTests.Each_api_key_has_its_own_limit` |
| 5 | Refused API keys were not logged | A09 | Each refusal logged with reason, path and address (never the key) | — |
| 6 | Unhandled exceptions outside Development had no defined response | A10 | `UseExceptionHandler` with problem details: a bare 500, no stack trace | — |

## Category by category

**A01:2025 Broken Access Control.**
- Every `/v1` and `/proxy` route requires an API key with the route's scope (`sign`, `verify`, `records:read`); a key without it gets 403.
- The tenant comes from the key. A record belongs to its tenant, and another tenant's id reads as 404, so ids cannot be probed. Tested: `ApiTests.Each_tenant_signs_with_its_own_key_and_reads_only_its_own_records`, and confirmed to fail when the tenant filter is removed.
- Proxy caches (`proxy_result`, `proxy_request`) are keyed by tenant.
- `/media/{id}` is deliberately unauthenticated, like the providers' own result URLs. The id is a ULID with 80 random bits, and the copy expires after 30 days (ADR 0008).

**A02:2025 Security Misconfiguration.**
- Findings 2, 3 and 6.
- The OpenAPI description is served only in Development.
- The development API key and the database seeding run only where `Database:MigrateAndSeedOnStartup` is set, which only the Development settings do (ADR 0006).
- `AllowedHosts` is `*`. **Open:** set it to the production host names at deploy.

**A03:2025 Software Supply Chain Failures.**
- Lockfiles for every ecosystem; c2pa-rs pinned exactly.
- Trivy on lockfiles and images daily, with a CycloneDX SBOM per run and a 90-day deferral limit (ADR 0009).
- Container base images limited to distroless (sidecar) and Debian slim with updates (worker).

**A04:2025 Cryptographic Failures.**
- API keys are 256 random bits, stored as SHA-256 only (ADR 0006).
- The claim signing key stays in AWS KMS; claims are signed with ES256 (ADR 0004).
- Internal channels use mutual TLS, with the API offering only TLS 1.3 (ADR 0010).
- Public TLS terminates at the hosting edge. **Open:** confirm TLS 1.3 at Fly.io's edge.

**A05:2025 Injection.**
- Database access is EF Core with parameterised queries throughout; no SQL is built from strings.
- Manifest definitions are built as JSON objects (`JsonObject`), not by concatenation, so the upload's file name cannot change the structure.
- No shell commands; no templates.
- ZAP's injection rules (SQL, OS command, SSTI, XXE, XPath) found nothing.

**A06:2025 Insecure Design.**
- Finding 4.
- Uploads are limited by Kestrel's request limit (30 MB) and to JPEG, PNG and WebP. Provider downloads in proxy mode are capped at 32 MB.
- Every signed output is validated before release, and output that cannot be signed fails the call rather than going out unsigned.
- **Open:** per-tenant quotas by tier (§4.6).

**A07:2025 Authentication Failures.**
- API keys are high-entropy, hashed, revocable and scoped. Brute force is impractical at 256 bits, and refusals are logged (finding 5).
- No passwords and no sessions in the API; the dashboard's sign-in is Supabase Auth (ADR 0002), outside this review.

**A08:2025 Software or Data Integrity Failures.**
- Signed outputs are hard-bound by their C2PA manifest and checked by c2pa-rs before release.
- In proxy mode, outputs are fetched only from the providers' media hosts: an allowlist, HTTPS only, no redirects followed.
- No deserialisation of untrusted objects beyond JSON.
- CI runs from pinned tool versions.

**A09:2025 Security Logging and Alerting Failures.**
- Finding 5.
- Internal failures are logged with the reference returned to the caller (finding 1).
- **Open:** alerting on repeated refusals and on signing failures, and a central log store (§4.9: Grafana Cloud).

**A10:2025 Mishandling of Exceptional Conditions.**
- Findings 1 and 6.
- Failures in the watermark worker, the sidecar or KMS stop the sign and return 4xx/5xx; nothing is released half-processed.
- A failure part-way through a multi-image proxy result fails the whole call, though earlier images in it stay signed and recorded (ADR 0008).

## Still open

- Set `AllowedHosts` and confirm TLS 1.3 at the public edge, at deploy.
- Alerting and central logs.
- Quotas by tier.
- Repeat this review and the ZAP scan against the deployed staging environment.
