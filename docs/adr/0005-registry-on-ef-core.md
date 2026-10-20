# ADR 0005 — Registry on EF Core

- **Status:** Accepted
- **Date:** 13 October 2026
- **Deciders:** Eoin

## Context

docs/ARCHITECTURE.md §1.4 defines the core tables and §1.1 sets the rule that the registry holds hashes, not media. ADR 0002 moved data to Supabase and left three things open: one migration tool or two, what local development runs against, and how tenant isolation meets the API's own schema. The first registry slice (tenant, tenant key, asset record) needed the first two answered.

## Decision

- **EF Core migrations own the application schema.** The context and migrations live in `src/Signunder.Api/Data`; the entities are plain classes in `Signunder.Core`. Table and column names are snake_case. `dotnet-ef` is a local tool (`dotnet-tools.json`).
- **Local development runs on the plain Postgres in `deploy/docker-compose.yml`.** The Supabase CLI's local stack is not adopted yet.
- **Record ids are ULIDs**, stored as 26 characters, so records sort by creation time and the id is short enough to print.
- **Hashes, not media.** An asset record stores the SHA-256 of the signed file and of the manifest store (hex text), the manifest label, actions, layers, the validation state and the crJSON validation report. It stores no image bytes.
- **In Development the API applies migrations and seeds a demo tenant at start-up**, taking the tenant's key from the `Signer` configuration. Nothing does this outside Development.
- **Schema `registry`, not `public` (added 15 Oct 2026).** Supabase serves the `public` schema through its REST API to anyone with the project's public key, so the registry's tables and EF's migration history live in `registry`. In Supabase the API connects as role `signunder_api`, which owns `registry`, has `search_path = registry`, at most 10 connections, and can create nothing in `public` (it cannot read the marketing site's `public.waitlist`). The four earlier migrations were squashed into one before anything was deployed.
- **The schema carries only columns that something fills.** The rest of §4.4 arrives as later migrations with the features that need it.

## Consequences

- The "one migration tool or two" question in ADR 0002 is settled for the application schema. Row-level security policies and anything in Supabase's `auth` schema are not covered and may still need Supabase migrations.
- Plain Postgres has no Supabase roles or policies, so tenant isolation by row-level security (§4.10) cannot be tested locally yet.
- How migrations run against the production database is undecided. Start-up migration runs where `Database:MigrateAndSeedOnStartup` is set: Development, and staging (ADR 0012).
- `/v1/sign` saves the record before it responds, so a database failure discards a file the sidecar has already signed. Acceptable while signing is synchronous; revisit with the job queue.
- New packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`, `Ulid`.

## Open

- Production migrations: a deploy step, a migration bundle, or generated SQL applied through Supabase.
- Where row-level security policies are defined and how they are tested.
- Whether hashes should be `bytea` once the fingerprint index exists.
