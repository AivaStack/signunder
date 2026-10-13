# ADR 0006 — API keys

- **Status:** Accepted
- **Date:** 13 October 2026
- **Deciders:** Eoin

## Context

ADR 0002 split authentication in two: the dashboard signs users in with Supabase Auth, and customer API keys remain the API's own. SIGNUNDER.md §4.4 sketches an `api_key` table. With the registry in place (ADR 0005), every request needed to resolve to a tenant.

## Decision

- **Format.** `su_live_` or `su_test_` followed by 32 random bytes in base64url. The prefix makes a leaked key recognisable to secret scanners and to people.
- **Storage.** Only the SHA-256 of the key is stored, with the first 12 characters as a display prefix. A fast hash is enough because the key is 256 random bits, not a password; a slow hash would add latency to every request and no security. The key is shown once, when it is made.
- **Transport.** `Authorization: Bearer <key>`.
- **Scopes.** Each key carries a list: `sign`, `verify`, `records:read`. An endpoint requires its scope; a key without it gets 403.
- **Tenant scoping in the API.** The tenant comes from the key. Signing uses that tenant's active key, and a record is visible only to its tenant: another tenant's record is reported as 404, so ids cannot be probed.
- **Revocation** is a timestamp on the row; a revoked key is refused like an unknown one.
- **`last_used_at`** is written at most once a minute per key.
- **Development key.** In Development the seed gives the demo tenant a fixed key from `appsettings.Development.json`. It is public by design, like the development signer (ADR 0003), and is seeded only where `Database:MigrateAndSeedOnStartup` is on.

## Consequences

- Isolation is enforced by the API's queries, and tested there. Row-level security in the database (§4.10) is a second layer still to add; ADR 0005 leaves where its policies live open.
- There is no interface for making or revoking a key yet. Until the dashboard exists it is a database operation.
- Each authenticated request costs one indexed lookup. No cache yet.
- A production deploy must never run in the Development environment, or the public key would be seeded.

## Open

- Rate limits and quotas per key and per tenant (§4.6).
- Key expiry and rotation.
- Whether `verify` should need a key at all, given the public verify page.
