---
title: "Does Fireworks AI sign your AI output? What its terms say"
description: "Fireworks AI attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: fireworks
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [fireworks, article-50, platforms, c2pa]
sources:
  - title: "Fireworks AI Terms of Service (PDF)"
    url: https://fireworks.ai/terms-of-service
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Fireworks AI add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Fireworks attaches no C2PA manifest and no watermark, and its terms make you entirely responsible for your content, a term that includes output."
  - q: "Who is responsible for Article 50(2) marking when I generate through Fireworks?"
    a: "You are, if the model sits inside your own user-facing product: the Commission's guidelines treat the integrator as the provider, and Fireworks' terms put output on you."
  - q: "Does Fireworks offer EU-region processing?"
    a: "Its data-residency documentation says to contact sales, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: "entirely responsible for your Content", where Content includes Output.
- EU region: contact sales.

## Does Fireworks AI attach a C2PA manifest or a watermark?

Fireworks' terms define "Content" as your Input and Output together, so the responsibility clause covers what the model generates for you. Nothing in its documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026. On 8 October the terms had read as silent on marking; the clause above is the general one, and there is still no AI-specific regulation clause.

## What do Fireworks AI's terms say about who is responsible?

The terms were last updated on 10 July 2026. Two clauses matter here:

> "You acknowledge and agree that you are entirely responsible for your Content, and for any harm or liability resulting from or arising out of that Content." — Fireworks AI Terms of Service (PDF), Content, definitions and responsibility, read 12 October 2026

> "Any Output obtained through your use of the Service is so obtained at your own risk" — Fireworks AI Terms of Service (PDF), Output, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Fireworks AI's terms point the same way. So if your users see images, audio or video that Fireworks AI generated for you, the marking obligation is yours, and it is not discharged by anything Fireworks AI attaches.

## What this means if you generate through Fireworks AI

Nothing arrives marked, and the terms define output as your content. If Fireworks is one of several providers behind your product, the record that names you has to be added after generation, consistently, whichever provider served the request.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Fireworks AI's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
