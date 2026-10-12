---
title: "Does Novita AI sign your AI output? What its terms say"
description: "Novita AI attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: novita
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [novita, article-50, platforms, c2pa]
sources:
  - title: "Novita AI Terms of Use"
    url: https://novita.ai/legal/terms-of-service
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Novita AI add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Novita attaches no C2PA manifest and no watermark, and its terms make you solely responsible for output and for complying with the laws of your jurisdiction."
  - q: "Who is responsible for Article 50(2) marking when I generate through Novita?"
    a: "You are, if the model sits inside your own user-facing product: the Commission's guidelines treat the integrator as the provider, and Novita's terms put output and compliance on you."
  - q: "Does Novita offer EU-region processing?"
    a: "German GPU zones are documented, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: "solely responsible for evaluating and verifying all output"; compliance with the laws of your jurisdiction.
- EU region: Germany GPU zones.

## Does Novita AI attach a C2PA manifest or a watermark?

Nothing in Novita's API reference or documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026; its image endpoints return a URL to the generated file and nothing about provenance.

## What do Novita AI's terms say about who is responsible?

The terms were last updated on 5 August 2026. Two clauses matter here:

> "YOU ARE SOLELY RESPONSIBLE FOR EVALUATING AND VERIFYING ALL OUTPUT BEFORE RELYING UPON IT." — Novita AI Terms of Use, Section 10.3, AI Output Disclaimer, read 12 October 2026

> "your use must comply with all applicable laws and regulations in your jurisdiction" — Novita AI Terms of Use, Section 3, User Representations, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Novita AI's terms point the same way. So if your users see images, audio or video that Novita AI generated for you, the marking obligation is yours, and it is not discharged by anything Novita AI attaches.

## What this means if you generate through Novita AI

German zones place the computation in the EU; the output still leaves unmarked. If your users are in the EU, that is the gap the December deadline is about.

Signunder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Novita AI's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. Signunder is not a law firm and this is not legal advice.*
