---
title: "Does fal.ai sign your AI output? What its terms say"
description: "fal.ai attaches a C2PA manifest signed as fal, plus an invisible watermark. Its terms put output use at your sole risk. Read 12 October 2026."
slug: fal
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [fal, article-50, platforms, c2pa]
sources:
  - title: "fal.ai Terms of Service"
    url: https://fal.ai/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
  - title: "fal.ai Verify page"
    url: https://fal.ai/verify
    read: 2026-10-12
faq:
  - q: "Does fal.ai add Content Credentials to generated images?"
    a: "Yes, for media generated through its hosted applications, signed as fal and with an invisible watermark, according to fal's Verify page read on 12 October 2026. Whether raw API output is signed is not documented."
  - q: "Whose name is on a fal-signed image?"
    a: "fal's. A verifier shows fal as the signer, not the company that resold the generation under its own brand. Signing under your own name needs your own conformance listing and certificate."
  - q: "Does fal.ai offer EU-region processing?"
    a: "Not that its pages state, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **Yes, signed as fal**, for its hosted applications; raw API scope undocumented.
- Watermark: **yes**, invisible, vendor not named.
- Terms: output used at the customer's sole risk; lawful use is the customer's duty.
- EU region: none stated.

## Does fal.ai attach a C2PA manifest or a watermark?

fal is the one platform of the eleven that documents provenance. Its Verify page says: "Every piece of media generated through fal's hosted applications is signed with Content Credentials (C2PA)" and that output is "embedded with an invisible watermark". Verification checks "whether fal was the signer". The page covers fal's hosted applications; whether raw API calls are signed is not stated anywhere we could find, and it was not stated on 8 October either.

## What do fal.ai's terms say about who is responsible?

The terms were last updated on 8 September 2026. Two clauses matter here:

> "Customer must use the AI Features and the Output Content only (i) in a lawful manner" — fal.ai Terms of Service, Section 4(c), AI Features, read 12 October 2026

> "USE OF SUCH OUTPUT CONTENT IS AT CUSTOMER'S SOLE RISK." — fal.ai Terms of Service, Section 16, Disclaimers, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). fal.ai's terms point the same way. So if your users see images, audio or video that fal.ai generated for you, the marking obligation is yours, and it is not discharged by anything fal.ai attaches.

## What this means if you generate through fal.ai

If you resell fal generation under your own brand, your outputs may already carry a manifest, but the signer is fal. A customer, a platform or a regulator reading that manifest sees your supplier, not you, and the watermark is fal's, not yours. That is better than nothing and it is not the record Article 50(2) puts on you, because you are the provider of the system your users see.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from fal.ai's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
