---
title: "Does DeepInfra sign your AI output? What its terms say"
description: "DeepInfra attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: deepinfra
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [deepinfra, article-50, platforms, c2pa]
sources:
  - title: "DeepInfra Terms of Service"
    url: https://deepinfra.com/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does DeepInfra add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, DeepInfra attaches no C2PA manifest and no watermark, and its terms make you solely responsible for your own products and end-user relationships."
  - q: "Who is responsible for Article 50(2) marking when I generate through DeepInfra?"
    a: "You are, if the model sits inside your own user-facing product: the Commission's guidelines treat the integrator as the provider, and DeepInfra's terms put your products and compliance on you."
  - q: "Does DeepInfra offer EU-region processing?"
    a: "Not that its pages state, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: "solely responsible for its own products, services, and end-user relationships"; compliance with all applicable laws.
- EU region: none stated.

## Does DeepInfra attach a C2PA manifest or a watermark?

Nothing in DeepInfra's documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026. Its image API is OpenAI-compatible and its model listings, including Google's image models served through DeepInfra, say nothing about provenance; whether a Google model served through DeepInfra carries Google's own signature is UNVERIFIED.

## What do DeepInfra's terms say about who is responsible?

The terms were last updated on 17 August 2026. Two clauses matter here:

> "it is solely responsible for its own products, services, and end-user relationships, for all Customer Data" — DeepInfra Terms of Service, Section 10(iii), Customer's Representations and Warranties, read 12 October 2026

> "it and its Users shall comply with all applicable laws, including data protection, privacy" — DeepInfra Terms of Service, Section 10(ii), Customer's Representations and Warranties, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). DeepInfra's terms point the same way. So if your users see images, audio or video that DeepInfra generated for you, the marking obligation is yours, and it is not discharged by anything DeepInfra attaches.

## What this means if you generate through DeepInfra

Unmarked output, and terms that frame your product and your end users as yours alone. The one open question is whether a third-party model that signs at source keeps its signature when served here; we could not confirm it, and a supplier's signature would in any case name the supplier.

Signunder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from DeepInfra's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. Signunder is not a law firm and this is not legal advice.*
