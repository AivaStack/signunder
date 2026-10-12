---
title: "Does WaveSpeedAI sign your AI output? What its terms say"
description: "WaveSpeedAI attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: wavespeed
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [wavespeed, article-50, platforms, c2pa]
sources:
  - title: "WaveSpeedAI Terms of Service"
    url: https://wavespeed.ai/static/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
  - title: "WaveSpeedAI, AI Watermark Remover page"
    url: https://wavespeed.ai/landing/watermark-remover/ai-watermark-remover
    read: 2026-10-12
faq:
  - q: "Does WaveSpeedAI add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, WaveSpeed attaches no C2PA manifest and no watermark, and its terms make the customer responsible for any content generation that violates the law."
  - q: "Who is responsible for Article 50(2) marking when I generate through WaveSpeed?"
    a: "You are, if the model sits inside your own user-facing product: the Commission's guidelines treat the integrator as the provider, and WaveSpeed's terms put output and lawful generation on you."
  - q: "Does WaveSpeed sell a watermark remover?"
    a: "Yes. Its own page describes an AI watermark remover for images or videos you own or are authorised to modify, as read on 12 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**; the platform sells a watermark remover.
- Terms: the customer "is responsible for their use of the Services, including any content generation that violates any Laws".
- EU region: none stated.

## Does WaveSpeedAI attach a C2PA manifest or a watermark?

Nothing in WaveSpeed's documentation mentions Content Credentials, C2PA or output marking, as searched on 12 October 2026. The platform does sell the opposite: an AI watermark remover, described on its own page as a tool that "estimates what could plausibly appear behind an overlay", offered for "images or videos you own or are authorized to modify". Section 13.4 of the terms notes that the law applying to the services "is evolving rapidly in various jurisdictions". The operating entities are a Singapore company and a Hong Kong company.

## What do WaveSpeedAI's terms say about who is responsible?

The terms were last updated on 28 September 2026. Two clauses matter here:

> "Customer is responsible for their use of the Services, including any content generation that violates any Laws." — WaveSpeedAI Terms of Service, Section 8.1, Acceptable and Restricted Use, read 12 October 2026

> "Customer is solely responsible for evaluating, validating, and verifying all Outputs." — WaveSpeedAI Terms of Service, Section 13.5, Disclaimer, read 12 October 2026

The terms also prohibit "misrepresenting or misleading people into believing that the use of the Services or Outputs is human-generated" (Section 8.3). That is a rule about deception, not a marking requirement; a signed manifest in your own name is the surest way to comply with it.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). WaveSpeedAI's terms point the same way. So if your users see images, audio or video that WaveSpeedAI generated for you, the marking obligation is yours, and it is not discharged by anything WaveSpeedAI attaches.

## What this means if you generate through WaveSpeedAI

Outputs are unmarked and the terms are explicit that unlawful generation is yours. The watermark-remover product is worth knowing about for a different reason: under California's AI Transparency Act, stripping provenance is itself a violation, and the EU Code of Practice expects providers to prohibit removal in their terms.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from WaveSpeedAI's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
