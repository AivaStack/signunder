---
title: "Does Together AI sign your AI output? What its terms say"
description: "Together AI attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: together
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [together, article-50, platforms, c2pa]
sources:
  - title: "Together AI Terms of Service"
    url: https://www.together.ai/terms-of-service
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Together AI add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Together attaches no C2PA manifest and no watermark, and its terms make you responsible for all of your content and output."
  - q: "Who is responsible for Article 50(2) marking when I generate through Together?"
    a: "You are, if the model sits inside your own user-facing product: the Commission's guidelines treat the integrator as the provider of the AI system, and Together's terms place output on you."
  - q: "Does Together offer EU-region processing?"
    a: "Only on dedicated endpoints, according to its data-centre support page read on 8 October 2026; the shared serverless endpoints are not EU-placed."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: "responsible for all of Your Content and Output".
- EU region: dedicated endpoints only.

## Does Together AI attach a C2PA manifest or a watermark?

Nothing in Together's documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026.

## What do Together AI's terms say about who is responsible?

The terms were last updated on 19 May 2026. Two clauses matter here:

> "You shall be responsible for all of Your Content and Output, including ensuring that it does not violate this Agreement." — Together AI Terms of Service, Section 3.1, Your Content, read 12 October 2026

> "It is your responsibility to comply with the terms and conditions of those models." — Together AI Terms of Service, Section 2.4, Your Responsibilities, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Together AI's terms point the same way. So if your users see images, audio or video that Together AI generated for you, the marking obligation is yours, and it is not discharged by anything Together AI attaches.

## What this means if you generate through Together AI

Outputs come back unmarked, and the terms say they are yours. If you route between Together and other providers, the inconsistency is the second problem: some outputs may carry a supplier's manifest and others nothing, which is why one record in your own name across all of them is the shape that holds up.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Together AI's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
