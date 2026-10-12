# ADR 0001 — Web front end: Zippay (Next.js) and React + shadcn/ui

- **Status:** Accepted
- **Date:** 8 October 2026
- **Deciders:** Eoin

## Context

SIGNUNDER.md §4.3 left the dashboard as "Blazor or a small TS front end" and said nothing about the marketing site. Eoin holds a shadcnblocks licence and wants to use the Zippay template (https://www.shadcnblocks.com/template/zippay): a 25-page marketing template with 100+ components, built on shadcn/ui, Tailwind 4 and TypeScript, sold in Next.js and Astro editions.

shadcn/ui components are React. A Blazor dashboard would mean re-creating the same look by hand and keeping two UI systems in step.

The build plan (§6, Phase 2) releases the core as open source. Licensed template code cannot be republished.

## Decision

1. **Marketing site:** Zippay, Next.js edition.
2. **Dashboard:** React + shadcn/ui, in TypeScript, calling the .NET API. Blazor is dropped.
3. **Repository:** the web front end lives in a separate private repository (`signunder-web`), not in this one.
4. **Theme:** green as the primary colour. The template already ships a green ramp (`--primary-100: #00E599`) in `src/app/globals.css`, so this needs no change unless a deeper, less neon green suits a compliance product better.

The downloaded edition is Zippay 1.0.0 (26 Sep 2025): Next.js 15.1.1, React 19, Tailwind 4, shadcn/ui, MDX for blog and integrations content. Shadcnblocks lists a 2.0.0 (19 Mar 2026) that adds an Astro edition; whether its Next.js edition carries a newer Next.js is unchecked.

## Consequences

- One design system across the marketing site, dashboard and public verify pages.
- The .NET surface is the API, domain, rules engine and SDK. No UI is written in .NET, so `Signunder.Dashboard` is removed from the planned layout.
- The dashboard needs a typed client for the .NET API; generate it from the API's OpenAPI document rather than writing it by hand.
- Next.js needs a Node host. §4.9 plans Cloudflare in front of two EU boxes and has no Node runtime today.
- This repository can be opened without rewriting history, because no template code ever enters it.

## Open

- Whether the dashboard is a route group inside the same Next.js application as the marketing site, or a separate application.
- Where Next.js is hosted (a container on the EU boxes, or a managed host with an EU region). The dashboard handles tenant sessions, so principle 5 in §4.1 (EU by default) applies to it.
- ~~The exact shadcnblocks licence wording~~ — **read 8 Oct 2026** from the `LICENSE` file in the purchased download. It grants an ongoing, non-exclusive, worldwide licence to build unlimited end products ("usually a new website"), and forbids re-distributing or re-selling the template "as stock, in a tool or template" or provisioning it "from a 'hosting' service, a 'website builder' or a SAAS product". Signunder's own site and dashboard are end products and are permitted. Keeping the template out of the open-source repository is required, not merely prudent.
- Whether the per-tenant verify pages are rendered by the Next.js application or by the .NET API with the shared Tailwind tokens.
