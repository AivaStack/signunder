# ADR 0002 — Hosting: Vercel and Supabase, with Fly.io for the back end

- **Status:** Accepted
- **Date:** 8 October 2026
- **Deciders:** Eoin

## Context

ADR 0001 put the marketing site and dashboard on Next.js and left its hosting open. SIGNUNDER.md §4.9 planned two EU boxes, a managed Postgres, Redis and an S3-compatible object store. Eoin wants to build and deploy on Vercel and to use Supabase.

Vercel runs Node and small Python functions. It does not run long-lived containers, and it limits request bodies to about 4.5 MB (*from memory*, UNVERIFIED). The .NET API, the Rust sign sidecar (c2pa-rs) and the Python watermark worker (TrustMark, which loads model weights) cannot run there as designed.

## Options considered

1. **Hybrid.** Vercel for the web front end, Supabase for data, a container host (Fly.io or Hetzner) for the back end.
2. **All-in on Vercel.** Rewrite the back end in TypeScript as Next.js functions on the official c2pa Node library. One platform and one language, but it discards the .NET skeleton, moves off Eoin's home stack, and watermarking inside a serverless function is untested.
3. **Web only.** Commit to Vercel for the site and leave everything else as written.

## Decision

Option 1.

- **Vercel:** the Next.js marketing site and dashboard (`signunder-web`). The template sets `output: 'export'`, so the marketing site is a **fully static export**: no server rendering, no API routes, no middleware, and therefore no functions to place in an EU region. Static assets are served from Vercel's global CDN, which is not personal data. Any function added later must be pinned to an EU region.
- **Supabase (EU region):** Postgres, Auth for dashboard sign-in, Storage for transient media and evidence retention, Supabase Queues (pgmq) for job dispatch in place of Redis.
- **Fly.io, Frankfurt (`fra`):** the .NET API, Rust sign sidecar and Python workers, as containers. Chosen over Hetzner: it deploys from the existing Dockerfiles with rolling releases, needs no server administration, and gives private networking between the API and the sidecar.
- **Unchanged:** AWS KMS for signing keys (*superseded 15 Oct 2026 by ADR 0011: Google Cloud KMS*), Cloudflare in front of the public API, verify and detect endpoints.

## Consequences

- Three platforms to operate instead of one, and three sub-processors to name in the DPA besides AWS. All four are US companies running EU regions; Hetzner was the only EU-owned option.
- **Email (added 9 Oct 2026).** `hello@signunder.com` is not a separate mailbox: signunder.com was added to the parent company's existing Google Workspace as a **domain alias** of aivastack.com, so addresses mirror into the existing inbox at no extra licence cost. DNS for it lives in Vercel: Google's single `smtp.google.com` MX, SPF, a 2048-bit DKIM key and DMARC. Google Workspace therefore joins the sub-processor list, since prospect correspondence lands there. Two sequencing constraints follow:
  - DMARC starts at `p=none` and only tightens to `p=reject` once outbound mail is confirmed to pass DKIM. Tightening first would have recipients reject our own mail.
  - Under `p=reject`, any transactional email (waitlist confirmations, later the API's notifications) sent through a provider such as Resend or Postmark will be rejected unless that provider's DKIM key and SPF include are added to signunder.com **before** the first send.
- Fly.io costs several times more per dedicated core than Hetzner (*from memory*, UNVERIFIED). The watermark worker is CPU-heavy and needs dedicated cores, so this matters at the load target and for video. The worker is a container and can move to Hetzner alone.
- Fly.io has had notable outages in past years (*from memory*, UNVERIFIED). Run at least two machines from the start and read its status history before offering an uptime commitment on the detect endpoint.
- The dashboard signs users in with Supabase Auth; the .NET API validates those JWTs. Customer API keys remain the API's own.
- Tenant isolation by row-level security (§4.10) is native to Supabase. The API connects with a role that is subject to those policies, never the service role, on tenant paths.
- Media uploads go directly to Supabase Storage by signed URL; `sign` receives an object reference. Nothing large passes through a Vercel function.
- Principle 5 in §4.1 (EU by default) holds for tenant data and processing. The marketing site's static assets are served from Vercel's global CDN.
- A static export cannot receive form posts, so the waitlist form posts to an endpoint supplied at build time (`NEXT_PUBLIC_WAITLIST_ENDPOINT`), intended to be a Supabase insert against an insert-only table. Until it is set, the form tells the visitor it is not connected rather than discarding the address silently.
- The dashboard needs sessions and server-side calls, which a static export cannot do. It therefore becomes either a second Next.js application or a reason to drop `output: 'export'`. This largely settles the "one app or two" question below in favour of two.
- `deploy/docker-compose.yml` still starts plain Postgres and Redis. It no longer mirrors production and needs revisiting.

## Open

- Whether Supabase Queues carries the job load at 100 signs a second (§5, Load), or Redis returns.
- One migration tool or two: EF Core for the application schema, Supabase migrations for policies and auth. *Settled for the application schema by ADR 0005 (EF Core); policies and auth remain open.*
- Local development: the Supabase CLI's local stack, or the existing compose file. *For now the compose file (ADR 0005).*
- Vercel and Supabase plan prices and the plan that includes point-in-time recovery; §3.3 carries unverified figures.
