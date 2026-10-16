# ADR 0013 — Self-serve accounts with Supabase Auth

- **Status:** Accepted
- **Date:** 16 October 2026
- **Deciders:** Eoin

## Context

A prospect should be able to try SignUnder without Eoin making a tenant and an API key by hand. ADR 0002 put the dashboard's sign-in on Supabase Auth, and API keys already exist (ADR 0006). The website is a static export on Vercel, so sign-in happens in the browser.

## Decision

- **Sign-in is Supabase Auth's email link**, called through its REST API from `signunder.com/dashboard`, with no client library. The link returns to `/dashboard` with the session in the URL fragment; the browser keeps it in local storage and refreshes it. Supabase's site URL is `https://signunder.com`, and links may return only to `/dashboard` there or on `localhost:3000` (set 16 Oct 2026).
- **The API trusts the session token directly.** Routes under `/v1/account` take only a Supabase session token from `SupabaseAuth:Issuer`, checked against the keys in its discovery document (ES256), audience `authenticated`, and `role` `authenticated`. An API key is not accepted there, and the API-key handler stays out of those routes.
- **Routes:** `GET /v1/account`; `POST /v1/account/tenant` (a company name, from which the slug is made); `POST /v1/account/keys`; `POST /v1/account/keys/{id}/revoke`.
- **One company per person** for now (`registry.tenant_member`, unique on the Supabase user id). Roles and invitations later.
- **New companies sign with the configured signer**, SignUnder's own key and certificate, until they are listed and have their own (SIGNUNDER.md §3.1, Cloud Free; §6, Phase 3).
- **Keys:** the first comes with the company; at most five active; the plaintext is shown once. Keys start `su_test_` unless `ApiKeys:Live` is set, which production will set.
- **Slugs** are lower-case ASCII from the name; a taken or reserved one (`demo`, `api`, `verify`, …) gets a random six-hex-digit suffix.
- **Browser origins** for every browser caller (verify page, dashboard) are `Website:AllowedOrigins`.

## Consequences

- Anyone with an email address can get a staging key. Signing costs about €0.000013 per signature in Cloud KMS (ADR 0011). Since 16 Oct 2026 the free tier's 2,000 signed outputs a calendar month (UTC; §3.1) are enforced (`Quotas:MonthlyOutputs:{tier}`; 25,000 for growth): signing answers 429 with the reset date, proxy mode refuses a new generation before the provider makes it, and the dashboard shows the month's count. The count is of registry records, so simultaneous signs can pass the limit by a few.
- Supabase's built-in mailer sends at most two emails an hour for the project (read in the project's Auth settings, `rate_limit_email_sent`, 16 Oct 2026). Enough for the first prospects; a custom SMTP provider is needed before any launch.
- The dashboard stores the signed-in person's email (Supabase Auth and `tenant_member`) and keeps the session in local storage. The privacy and cookie pages do not say so yet.
- The flow was tested with tokens signed by a stand-in key, including forged, foreign-issuer, wrong-audience, expired and anonymous ones, and end to end by Eoin on 16 Oct 2026: email sign-in on signunder.com, company created, key issued, a PNG signed with it and recognised on the company's verify page by its manifest.

## Open

- Custom SMTP; Stripe.
- Several people per company.
- Whether production keeps one Supabase project for marketing and accounts.
