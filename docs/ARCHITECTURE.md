# SignUnder — architecture

*How SignUnder is built: the architecture, the testing approach, and the log of technical decisions. Each decision with lasting consequences also has a record in [`docs/adr/`](adr/). Everything dated was read from a primary source on that date unless marked UNVERIFIED or* from memory*.*

---

## 1. Architecture

### 1.1 Principles

1. **The customer's key, the customer's name, the customer's record.** SignUnder never holds a customer's private key in software; keys live in a cloud KMS under a per-tenant key policy, signing is a KMS `Sign` call, and the certificate's subject is the customer. SignUnder's own certificate signs only interim (Cloud Free) output, with a CAWG identity assertion naming the customer.
2. **Preserve, then add.** Upstream manifests (fal, BFL, Bria) become ingredients. SignUnder adds its layer; it never strips.
3. **Hashes, not media.** The registry stores fingerprints and manifest hashes. Media is held only transiently for processing unless the customer enables evidence retention. Proxy mode is the exception that proves the rule: it keeps each signed copy for up to 30 days so the customer can fetch it (ADR 0008).
4. **The canonical implementation signs.** c2pa-rs is the reference; SignUnder wraps it rather than re-implementing manifest construction.
5. **EU by default.** All tenant data and processing in EU regions; US region optional later.
6. **Rules are data.** Jurisdiction and platform policies are versioned records with effective dates, not code paths.

### 1.2 System overview

```
                 ┌──────────────────────────────────────────────────────────────┐
                 │                        SignUnder (EU)                        │
  Customer app   │                                                              │
  ──SDK/HTTP──►  │  API (.NET)  ──►  Queue  ──►  Workers                        │
                 │   auth, quotas,        │        ├─ Sign worker (Rust sidecar: c2pa-rs)│
  Customer app   │   rules eval,          │        ├─ Watermark worker (Python: TrustMark)│
  ──proxy mode─► │   registry writes      │        ├─ Fingerprint worker (PDQ / pHash)    │
   (fal/Replicate│                        │        └─ Video worker (ffmpeg + per-frame WM)│
    passthrough) │        │                                   │                 │
                 │        ▼                                   ▼                 │
                 │   Postgres (tenants, keys-meta, rules, registry, audit)       │
                 │   Object store (transient media; optional evidence retention) │
                 │   KMS (per-tenant signing keys; SignUnder interim key)       │
                 │   TSA client (RFC 3161)                                       │
                 │                                                              │
  Public ─────►  │  verify.<tenant>.signunder.com  /  detect API  /  checker   │
  Customer ───►  │  Dashboard: onboarding wizard, keys, rules, usage, evidence  │
                 └──────────────────────────────────────────────────────────────┘
```

**Language split.** Control plane, API, SDK and rules engine in **.NET** (Eoin's lane; the .NET SDK is a differentiator nobody has). Signing in a thin **Rust** sidecar over gRPC wrapping c2pa-rs (the reference implementation; avoids binding drift). Watermarking in a **Python** worker (TrustMark is Python; swap for a licensed mark later behind the same interface). Everything containerised; one `docker compose up` for local.

### 1.3 Components

**API (.NET 10, minimal APIs).** Endpoints in §1.6. Responsibilities: tenant auth (API keys for the API; Supabase Auth for the dashboard, whose JWTs the API validates), quota and tier enforcement, rules evaluation per request, job dispatch, registry writes, audit log. Stateless; horizontally scalable.

**Sign sidecar (Rust, c2pa-rs).** Input: asset bytes, manifest definition (JSON), signer config. Signer is a KMS-backed `Signer` implementation: the sidecar computes the signature request and calls KMS `Sign` (ES256/PS256) — the private key never leaves KMS. Attaches the certificate chain, RFC 3161 timestamp, assertions, ingredients. Output: signed asset + manifest store bytes + `.crjson`-style report for conformance evidence. Also runs validation (the same library) so every output is verified before it leaves.

**Watermark worker (Python).** TrustMark encode/decode for images (Apache 2.0, Adobe; robust to resize, JPEG compression, modest crops; carries 61 bits with its strongest standard error correction, measured 13 Oct 2026 — not enough for a 128-bit record id, so each record gets its own random payload; ADR 0007). Audio: start with Resemble-compatible SynthID detection only (we can't embed SynthID); evaluate AudioSeal (Meta, open) for embedding. Video: per-frame TrustMark via ffmpeg filter graph, keyframe-sampled; Phase 4.

**Fingerprint worker.** PDQ (Meta's open perceptual hash) for images; TMK+PDQF or frame-sampled PDQ for video; chromaprint-style for audio later. Stored in Postgres with a vector/Hamming index (pgvector or a bit-hamming extension) for `detect`.

**Rules engine (.NET).** Evaluates a request's context (modality, provider, destination jurisdictions, audience, realism flag, intended platform) against rule-pack versions effective on the date; returns required layers, label strings, icons and a coverage verdict. Backs the public checker.

**Onboarding service (.NET).** The wizard: collects the customer's architecture answers, renders the C2PA security-architecture document (Appendix C template) and generates the sample signed files per media type via the sign sidecar in "evidence mode"; tracks application status; on listing, captures the CPL record ID, drives the SSL.com certificate request, runs the key ceremony (create KMS key → CSR → cert install → activate tenant signer), and schedules renewal at day 330.

**Verify / detect (public, per tenant).** Reads manifests, decodes watermarks, looks up fingerprints; renders the public page; JSON for integrators. Rate-limited, cached, CDN-fronted. The detect endpoint's response format follows the Code's interoperable-detection spec when published.

**Dashboard (React + shadcn/ui, TypeScript; ADR 0001).** Lives with the marketing site (Zippay template, Next.js, green theme) in the private `signunder-web` repository and calls the .NET API. Keys and certs, onboarding progress, rules and coverage, usage, evidence pack export, Transparency Center settings.

**SDKs.** .NET first (NuGet), then TypeScript and Python. Two modes: *call* (`client.Sign(bytes, options)`) and *proxy* (an HTTP proxy that sits in front of fal/Replicate/Together base URLs and signs responses in flight — the customer changes a base URL and adds one header, `X-SignUnder-Key`; not zero code change, as first written. ADR 0008).

### 1.4 Data model (core tables)

```
tenant(id, legal_name, slug, region, tier, created_at)                      -- built
  + cpl_record_id, cpl_status                                                -- planned
tenant_key(id, tenant_id, kms_key_arn, algorithm, cert_chain_pem,
           status[pending|active|retired], created_at)                       -- built
  + cert_not_after, assurance_level                                          -- planned
asset_record(id ulid, tenant_id, tenant_key_id, created_at, modality, mime, size,
             bytes_sha256, manifest_hash, manifest_label, validation_state,
             actions jsonb, layers jsonb {manifest, watermark, fingerprint},
             validation_report jsonb, wm_payload bigint unique, fingerprint bytea,
             fingerprint_quality int, provider, model_name,
             retained_media_key nullable)                                    -- built
  + ingredients jsonb, model_version, rule_pack_versions jsonb               -- planned
proxy_result(tenant_id, provider, upstream_id, body jsonb, created_at)      -- built (ADR 0008)
rule_pack(id, jurisdiction_or_platform, version, effective_from, effective_to,
          source_url, rules jsonb, changelog text, author, created_at)
rule_evaluation(id, tenant_id, asset_record_id, pack_versions jsonb, verdict jsonb)
onboarding(tenant_id, step, answers jsonb, doc_render_key, samples jsonb,
           submitted_at, listed_at, notes)
audit_log(id, tenant_id, actor, action, subject, at, ip, detail jsonb)   -- append-only
api_key(id, tenant_id, prefix, hash, scopes text[], created_at, last_used_at,
        revoked_at)                                                          -- built (ADR 0006)
usage(tenant_id, period, outputs, video_seconds, verify_calls, detect_calls)
```

Tables marked *built* exist as of 13 Oct 2026 (ADR 0005); the rest are the design. `cert_chain_pem` holds the signing certificate first, replacing the separate `cert_pem`; hashes are hex text.

### 1.5 Key management

- *15 Oct 2026 (ADR 0011):* "KMS" in this section means **Google Cloud KMS** (HSM protection level, `europe-west3`), not AWS KMS. Per-tenant keys are key versions in key rings SignUnder controls; IAM on the key replaces AWS key policies.
- One KMS asymmetric key per tenant (ECC P-256 or RSA-2048/PS256 per the Certificate Policy's allowed algorithms — confirm in Appendix of CP v0.2), `Sign`-only key policy scoped to the tenant's signer role; SignUnder operators cannot export or use it outside the sign path.
- Certificate stored as metadata; private key never leaves KMS. Rotation: new key + new cert at renewal; old cert retained for validation of historic manifests.
- Level 1 by default (KMS satisfies "no stipulation" trivially). Level 2 (Scale tier): document KMS HSM backing and attestation per Security Requirements v0.2 §6.2.2, or an accredited audit — decide after reading the exact L2 attestation list for cloud KMS.
- SignUnder's own interim key: same scheme, SignUnder's cert; every interim manifest carries a CAWG identity assertion for the customer and a visible `interim: true` field in the registry.
- Key compromise drill: revoke cert, retire key, re-sign nothing (history stands, validated against the retired cert's validity period), notify tenant, record in audit.

### 1.6 API (v1)

```
POST /v1/sign                 multipart or JSON{url}; options: layers, actions, ingredients, destination, retain
                              → 202 {job_id} | 200 {asset_url, record_id, manifest_hash, layers, verdict}
GET  /v1/jobs/{id}
POST /v1/resign               asset previously signed + edit actions → new generation of the record
POST /v1/verify               asset → {signed_by, cert_subject, trust_status, issued_at, actions, ingredients, altered}
POST /v1/detect               asset → {ours: bool, record_id?, method: manifest|watermark|fingerprint, confidence}
GET  /v1/records/{id}
POST /v1/rules/evaluate       context → {required_layers, labels, icons, covered: bool, packs: [...]}
GET  /v1/rules/packs          list with versions and effective dates
GET  /v1/onboarding           status;  POST /v1/onboarding/answers;  POST /v1/onboarding/submit
GET  /v1/evidence/export      period → signed CSV/JSON pack
Public:
GET  https://verify.<tenant>.signunder.com/?id=... | POST upload   (page + JSON)
                                     (built 15 Oct 2026 as signunder.com/verify?t=<tenant>&id=..., over
                                      GET /public/v1/{tenant}/records/{id} and POST /public/v1/{tenant}/verify)
GET  https://signunder.com/checker                                 (free rules checker)
Proxy mode:
ANY  https://proxy.signunder.com/fal/*  (and replicate, together) → upstream, sign response, return
```

Idempotency keys on `sign`; webhooks for job completion; per-tenant rate limits; all responses carry `rule_pack_versions` so evidence is reproducible.

### 1.7 Manifest construction

- `c2pa.actions`: `c2pa.created` (digitalSourceType `trainedAlgorithmicMedia`), with `softwareAgent` = the customer's product name and version, model name/version in parameters; `c2pa.edited`/`c2pa.resized` etc. on `resign`.
- Ingredients: any upstream manifest found (fal, BFL, Bria) attached with its validation status; never removed.
- `c2pa.training-mining` assertion as the customer configures.
- CAWG identity assertion on interim-signed assets.
- Soft binding: the watermark payload (registry ID) recorded as a `c2pa.soft-binding` assertion so manifest and watermark cross-reference, per the Deployment Guidance's durable pattern.
- Timestamp from an RFC 3161 TSA on every manifest.
- Validate with c2pa-rs after signing; reject on any failure; store the validation report with the record.

### 1.8 Rule packs (shape)

```json
{
  "pack": "eu-ai-act-art50", "version": "2026.10", "effective_from": "2026-08-02",
  "source": ["https://eur-lex...", "Code of Practice 2026-06-10", "Guidelines 2026-07-20"],
  "rules": [
    { "id": "50-2-marking", "applies": {"role": "provider", "modality": ["image","video","audio","text"]},
      "requires": {"manifest": true, "watermark": "required|where_feasible", "detection_endpoint_by": "2027-02-02"},
      "grace": {"on_market_before": "2026-08-02", "until": "2026-12-02"},
      "text_threshold_tokens": 200 },
    { "id": "50-4-deepfake-label", "applies": {"realism": true, "depicts": ["person","place","event"]},
      "requires": {"visible_label": true, "label_text": {"en": "...", "de": "..."}, "icon": "eu-ai-icon-v1"} }
  ],
  "changelog": "2026.10: ..."
}
```

Platform packs follow the same shape with `requires.display_behaviour` (label / limit / reject) and the dated policy source. Each change is a new version; evaluations record which versions applied.

### 1.9 Infrastructure

- **Web:** Vercel hosts the Next.js marketing site and dashboard (`signunder-web`, ADR 0001). The marketing site is a fully static export (`output: 'export'`): no server rendering, no API routes, no functions, so nothing to place in an EU region; static assets come from Vercel's global CDN. The dashboard needs sessions and will be a second application, or will require dropping static export (ADR 0002).
- **Data:** Supabase project `SignUnder` (ref `lqxxcrkimjbysfqmedmt`) in eu-central-1 Frankfurt, AivaStack org, micro instance (nano is not available on paid plans): Postgres (row-level security for tenant isolation, pgvector for the fingerprint index), Auth for dashboard sign-in, Storage (S3-compatible) for transient media and evidence retention, and Supabase Queues (pgmq) for job dispatch in place of Redis.
- **Compute:** the .NET API, Rust sign sidecar and Python workers run as containers on Fly.io in Frankfurt (`fra`), at least two machines for the public endpoints, with Fly private networking between the API and the sidecar. The watermark worker needs dedicated rather than shared CPUs; if that cost bites at volume or for video, the worker alone can move to Hetzner. Vercel cannot run them: no long-lived containers, and a ~4.5 MB request-body limit (*from memory*, UNVERIFIED). Cloudflare in front of the public API, verify and detect endpoints.
- **Uploads:** large media goes from the client straight to Supabase Storage by signed URL, then `sign` is called with the object reference; nothing large passes through a Vercel function.
- **KMS:** Google Cloud KMS, `europe-west3` (Frankfurt), HSM protection level, in a project used only for SignUnder; Fly.io machines reach it by workload identity federation, without stored keys. No AWS account (ADR 0011, 15 Oct 2026; previously AWS KMS).
- **TSA:** SSL.com's bundled timestamps initially; DigiCert/GlobalSign TSA as paid fallback.
- **Observability:** OpenTelemetry → Grafana Cloud free tier; Sentry; a public status page.
- **CI/CD:** GitHub Actions; container images; rolling deploy on Fly.io; Vercel builds `signunder-web` from its own repo; migrations via EF Core against Supabase Postgres (ADR 0002 leaves the split with Supabase's own migrations open).
- **Backups:** Supabase daily backups, point-in-time recovery as a paid add-on (UNVERIFIED which plan); versioning on the evidence bucket.

### 1.10 Security and compliance

- Threat model: key misuse (mitigated by KMS policy + audit), tenant isolation (row-level security on `tenant_id`, per-tenant key policies), manifest forgery (we sign only what we processed; validation before release), watermark stripping (fingerprint fallback), SSRF via `url` inputs (allowlist + egress proxy), supply chain (pinned c2pa-rs, SBOM; built 14 Oct 2026: Trivy on lockfiles and images daily, CycloneDX SBOM per run, 90-day deferral limit, ADR 0009).
- Data: GDPR processor; DPA template naming Vercel, Supabase, Google Cloud (KMS; ADR 0011, replacing AWS), Fly.io, Cloudflare (Web Analytics: cookieless page-view counts on the marketing site, since 12 Oct 2026), Google (Analytics 4 on the marketing site, since 12 Oct 2026, loaded only after the visitor consents in a banner; Google is EU–US Data Privacy Framework certified), Google Workspace and Resend (which carries the waitlist notification, so a signup address leaves the EU until signunder.com is verified with it) (which carries `hello@signunder.com` as a domain alias of aivastack.com, so prospect correspondence sits in the parent company's mailbox) as sub-processors; EU-only processing of tenant data (the marketing site's static assets are served globally); media not retained by default; 30-day transient TTL; deletion API.
- The live site carries a real privacy notice (9 Oct 2026, updated 12 Oct 2026) naming Cloudflare, Google Analytics, Vercel, Supabase, Google Workspace and Resend as processors, a cookie page listing the two Google Analytics cookies set after consent, with waitlist addresses deleted on request and in any case within two years. The terms page (12 Oct 2026) covers website use only: not legal advice, written content under CC BY 4.0, Irish law; service terms are a separate document due before the first customer.
- Conformance evidence for SignUnder itself: the security-architecture document is a living artefact in `/docs/conformance/`, regenerated from the same wizard customers use (dogfood).
- SOC 2 Type I in the base scenario; ISO 27001 not before enterprise demand.

### 1.11 Repository layout

```
signunder/
  docs/                 this file, ADRs, conformance evidence, rule-pack sources
  content/              blog posts and reference pages (law, platforms, labels, guides, compare) as Markdown; published by signunder-web
  src/
    Signunder.Api/         .NET minimal API, auth, quotas, rules eval, jobs
    Signunder.Core/        domain: tenants, records, rules engine, onboarding state machine
    Signunder.Rules/       rule packs as versioned JSON + loader + tests
    Signunder.Sdk/         .NET SDK (NuGet) incl. proxy-mode handler
    sign-sidecar/       Rust: c2pa-rs wrapper, gRPC, KMS signer, validation, evidence mode
    wm-worker/          Python: TrustMark encode/decode, AudioSeal eval, PDQ
    video-worker/       Python/ffmpeg (Phase 4)
  sdks/
    typescript/  python/
  (web front end: separate private repo `signunder-web`, see ADR 0001)
  deploy/
    docker-compose.yml  (api, sidecar, wm-worker, postgres, redis, s3 stand-in, localkms)
    terraform/          (EU hosting, KMS, buckets, Cloudflare)
  tests/
    conformance/        sample generation + .crjson diffing against program examples
    robustness/         watermark survival suite (resize, crop, jpeg q, screenshot, re-encode)
    platform-survival/  manual scripts + results log (TikTok, Meta, LinkedIn, Adobe Verify)
    load/               k6 scripts
```

Local dev: `docker compose up` brings everything up with a local KMS stand-in (software signer behind the same interface, clearly non-conformant) and a seeded demo tenant; `make sample` signs a JPEG and a PNG through the running stack and saves each with the sidecar's crJSON validation report in `docs/conformance/samples/` (WAV and `c2patool` validation to follow); `make robustness` runs the survival suite and prints a table.

---

## 2. Testing

| Suite | What it proves | Tooling |
|---|---|---|
| **Conformance evidence** | Every media type produces a manifest that validates under c2pa-rs and `c2patool`, with the assertions the program expects; the `.crjson` matches the program's examples | c2patool, c2pa-rs validation, golden files |
| **Verifier display** | Signed samples show the expected state in Adobe Verify / Content Authenticity Inspect: own-name once listed, "Unrecognized" before (so we never mislabel interim) | manual + screenshots logged |
| **Watermark robustness** | TrustMark payload survives resize to 50%, JPEG q=50, 10% crop, screenshot, PNG→WebP; report survival rate per transform. Measured 13 Oct 2026 on four photographs: the watermark survived all five; the PDQ fingerprint survives resizing and recompression but **not** cropping or a framed screenshot, so it is no fallback there (ADR 0007) | pytest + Pillow/ffmpeg |
| **Platform survival** | Upload signed assets to TikTok, Instagram, LinkedIn, X, YouTube; record which layers survive (expect: manifest stripped on most, watermark survives, fingerprint always) | manual script + results table, re-run monthly |
| **Rules** | Every pack version has fixtures: context → expected verdict; effective-date boundaries; the checker's answers are reproducible | xUnit |
| **Key path** | KMS policy denies export/usage outside the signer role; rotation keeps historic validation green; compromise drill runbook executed | integration tests against real KMS in a test account |
| **Load** | 100 image signs/s sustained on the base Fly.io machines; p95 < 800 ms excluding upload | k6 |
| **Tenant isolation** | Cross-tenant access attempts fail at RLS and at key policy | integration |

---

---

## 3. Decision log

Technical decisions, newest last. The reasoning for each is in its ADR where there is one.

| Date | Decision | Basis |
|---|---|---|
| 8 Oct 2026 | **Web front end**: marketing site on the Zippay template (Next.js edition, green theme); dashboard in React + shadcn/ui, Blazor dropped; both in a separate private repo so licensed template code stays out of the open-source core | ADR 0001 |
| 8 Oct 2026 | **Hosting: hybrid.** Vercel for the Next.js site and dashboard; Supabase (EU) for Postgres, auth, storage and the queue; the .NET API, Rust sidecar and Python workers stay as containers on one small EU host. An all-TypeScript rewrite to fit inside Vercel was considered and rejected | ADR 0002 |
| 8 Oct 2026 | **Container host: Fly.io, Frankfurt.** Chosen over Hetzner for deploys straight from the Dockerfiles and no server administration. Accepted: higher cost per dedicated core, a past outage record (*from memory*, UNVERIFIED), and all four providers being US-owned | ADR 0002 |
| 13 Oct 2026 | **Development signer**: a committed, public ES256 key and private root, loaded by the sidecar only when `SIGNUNDER_DEV_SIGNER_DIR` is set and used only for `key_ref = dev:local`; never in the image. Validation reports are crJSON | ADR 0003 |
| 13 Oct 2026 | **Sidecar contract gains `ValidationState`** on the sign and validate responses, because crJSON carries per-manifest results but no overall verdict. First slice of `POST /v1/verify` built on it: summary of the active manifest plus the full report; ingredients not yet summarised | `sign.proto`, ADR 0004 |
| 13 Oct 2026 | **Registry on EF Core.** Tenant, tenant key and asset record tables on plain Postgres locally; EF Core migrations own the application schema; ULID record ids; a record on every sign holding hashes and the validation report, never media; `GET /v1/records/{id}`. No tenant auth yet | ADR 0005 |
| 13 Oct 2026 | **API keys.** `su_live_`/`su_test_` keys of 256 random bits, stored only as SHA-256, with scopes (`sign`, `verify`, `records:read`); the tenant comes from the key; another tenant's record reads as 404. Endpoint tests run the API against Postgres in a container | ADR 0006 |
| 13 Oct 2026 | **.NET SDK, call mode.** `SignunderClient` over sign, verify and records, package id `Signunder`, built for .NET 8 and .NET 10, safe for trimmed and native AOT apps. Proxy mode not built; nothing published; licence and default API address undecided | `src/Signunder.Sdk` |
| 13 Oct 2026 | **Watermark and fingerprint.** TrustMark Q with a random 61-bit payload per record, embedded before signing and bound into the manifest as `c2pa.soft-binding`; PDQ fingerprint stored with the record; Python worker over gRPC. Robustness suite passing on four photographs; PDQ found not to survive cropping. `detect` not built | ADR 0007 |
| 14 Oct 2026 | **Proxy mode for fal and Replicate.** The customer's provider key passes through; the SignUnder key goes in `X-SignUnder-Key`; finished results are signed once, cached, and their image URLs replaced by SignUnder-hosted copies kept up to 30 days; fail closed on anything unsignable; WebP added. Tested against documentation-shaped stand-ins only | ADR 0008 |
| 14 Oct 2026 | **Conformance application started.** Program v0.2 documents read; SignUnder's manifests fixed to pass the program's own asset rubric (31 of 31, spec 2.4). Intended intake: Generator Product, spec 2.4, Assurance Level 1, Backend class, generate JPEG/PNG/WebP, validate none. The security architecture document must describe production, so it follows the EU deploy and CI vulnerability scanning | §2.3 |
| 14 Oct 2026 | **Vulnerability scanning in CI.** Trivy on lockfiles (every push, daily) and service images (daily); fail on HIGH/CRITICAL, images only where a fix exists; deferrals expire within 90 days; distroless sidecar and Debian 13 worker. Security architecture document drafted with its gaps listed (§8 item 13) | ADR 0009 |
| 14 Oct 2026 | **Internal mutual TLS.** A private CA for the API (client), sidecar and watermark worker (servers); servers accept only the API's certificate; the API offers only TLS 1.3; a public development set runs in the local stack | ADR 0010 |
| 14 Oct 2026 | **KMS-signed certificate requests and an OWASP Top 10:2025 review.** `kms-csr` writes the claim-signing request with KMS's signature, to the program's AL1 request profile. OWASP review with a ZAP API scan found six issues, all fixed: internal error details hidden behind a reference, security headers, `no-store` on authenticated responses, rate limiting at **300 requests a minute per API key** by default, logging of refused keys, defined handling of unhandled errors | `docs/conformance/owasp-review.md` |
| 15 Oct 2026 | **Google Cloud KMS instead of AWS KMS.** Eoin prefers no AWS account. Production keys: Cloud KMS, HSM protection level, Frankfurt, own project; workload identity federation from Fly.io. The sidecar keeps the AWS API only for the local-kms emulator. Cloud HSM key attestations may serve Level 2 | ADR 0011 |
| 15 Oct 2026 | **Staging deployed on Fly.io**: three apps in Frankfurt, one machine each, suspended when idle (under $1 a month idle, ~$23 always on); keyless signing from Fly.io through Google workload identity federation; registry in Supabase as role `signunder_api` in schema `registry`. First deploy fixed the worker's slow model load and dead connections to suspended services; signed copies now expire through an API sweep | ADR 0012 |
| 15 Oct 2026 | **Public verify page.** `GET /public/v1/{tenant}/records/{id}` and `POST /public/v1/{tenant}/verify`, no key, 30 a minute per address, CORS for `signunder.com`; it finds the tenant's own outputs by manifest, then watermark, then PDQ fingerprint (31 bits or less), and says nothing about other images. Served at `signunder.com/verify?t=…` for now; `verify.<tenant>.signunder.com` (§4.6) needs DNS per tenant and comes later. The fingerprint search scans the tenant's records; an index replaces it before tenants have many thousands | ADR 0007, ADR 0012 |
| 16 Oct 2026 | **Self-serve accounts.** `signunder.com/dashboard`: sign in by Supabase Auth email link, name the company, get an API key (shown once; up to five; revocable). The API's `/v1/account` routes take only Supabase session tokens. One company per person; new companies sign with SignUnder's configured signer until listed. Built-in mailer allows two emails an hour | ADR 0013 |
| 18 Oct 2026 | **Rules engine and the free checker.** Rule packs as versioned JSON in `Signunder.Rules` (each rule cites its source and read date; a malformed pack fails to load), evaluated by `POST /public/v1/rules/evaluate` (no key; answers and date in, findings, requirements and pack versions out) and listed at `GET /public/v1/rules/packs`; `signunder.com/checker` asks and links a reproducible answer. First release: EU Article 50, the Code of Practice, California, and TikTok, Meta, YouTube, Google and LinkedIn; China, South Korea, India and New York next. Reviewed by Eoin: platform rules take the date the policy was read as their start; the Code's measures get no 2 December grace; California's platform, hosting and device duties and Article 50(1) are left out; exceptions are named, not assessed | — |
| 19 Oct 2026 | **Coverage widened, sources watched, Python SDK.** Rule packs added for mainland China, South Korea, India and New York (reviewed by Eoin); 20 static answer pages and the packs as JSON on signunder.com. A weekly GitHub workflow compares every pack source with its accepted snapshot and opens a `rules-watch` issue on change; 8 sources that refuse automated reads are listed for monthly checks by hand. Python SDK `signunder` 0.1.0 on PyPI, Apache-2.0, published by trusted publishing; the rest of the repository has no licence yet | — |
| 19 Oct 2026 | **The company's name inside its signatures.** A CAWG identity assertion (role `cawg.publisher`) names the company in manifests SignUnder signs: "Published by Acme Ltd (acme.com, verified by SignUnder)". The company proves its domain by a DNS TXT record from the dashboard and Eoin approves the name by hand; the credential is an X.509 certificate from SignUnder's own Identity CA, with the CA and the identity-signing key in Cloud KMS HSM (about €4.40 a month). Validators see it as well-formed but untrusted, since no shared identity trust list includes the CA | ADR 0014 |
| 20 Oct 2026 | **Repository opened under Apache-2.0.** The product, market and business plan, the marketing plan, the AI visibility panel and the source watch's third-party snapshots moved to the private `AivaStack/signunder-private`, with the workflows whose issues name customers or outages (identity reviews, uptime, rules watch); history rewritten without them. The architecture, testing and technical decisions came here | — |
| 20 Oct 2026 | **Billing.** Growth bought from the dashboard: by invoice for now (requests listed daily in the private repo, plan granted with `tools/set-plan.py`), Stripe Checkout, portal and signed webhooks ready behind `Billing:Provider`. Growth keeps signing past its allowance and counts the overage | ADR 0015 |
| 20 Oct 2026 | **Sign-in email through Resend.** Supabase Auth sends sign-in links through Resend's SMTP server (EU region) as `signin@signunder.com`, with a send-only key limited to signunder.com; the limit rises from two emails an hour to 30. DKIM and return-path records for Resend are in signunder.com's DNS | ADR 0013 |
