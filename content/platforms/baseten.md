---
title: "Does Baseten sign your AI output? What its terms say"
description: "Baseten attaches no C2PA manifest and no watermark. Its terms hand every developer, provider and deployer obligation to you. Read 12 October 2026."
slug: baseten
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [baseten, article-50, platforms, c2pa]
sources:
  - title: "Baseten Terms and Conditions"
    url: https://www.baseten.co/terms-and-conditions/
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Baseten add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Baseten attaches no C2PA manifest and no watermark. Its terms assign every developer, provider and deployer obligation to the customer."
  - q: "Who is responsible for Article 50(2) marking when I generate through Baseten?"
    a: "You are, and Baseten's terms say so using the AI Act's own words: the customer assumes all obligations imposed on the developer, provider or deployer of AI systems."
  - q: "Does Baseten offer EU-region processing?"
    a: "Yes, an EU region is documented, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: the customer "assumes and shall perform all obligations imposed on the developer, provider, and/or deployer of AI systems".
- EU region: yes.

## Does Baseten attach a C2PA manifest or a watermark?

Baseten's terms are the most explicit of the eleven: they name the AI Act's own roles and hand their obligations to the customer. Nothing in its documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026.

## What do Baseten's terms say about who is responsible?

The terms were last updated on 15 September 2026. Two clauses matter here:

> "Customer assumes and shall perform all obligations imposed on the developer, provider, and/or deployer of AI systems" — Baseten Terms and Conditions, Section 3.1, Customer's Responsibilities as Developer, Provider, and/or Deployer, read 12 October 2026

> "Customer assumes and shall perform all obligations applicable to a deployer or provider under applicable law" — Baseten Terms and Conditions, Section 4.4(e), Third-Party Products, read 12 October 2026

The terms also prohibit "not represent AI-generated content as human-generated" (Section 3.2, High-Risk Use Cases). That is a rule about deception, not a marking requirement; a signed manifest in your own name is the surest way to comply with it.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Baseten's terms point the same way. So if your users see images, audio or video that Baseten generated for you, the marking obligation is yours, and it is not discharged by anything Baseten attaches.

## What this means if you generate through Baseten

Baseten has written the allocation down. Whatever Article 50(2) asks of a provider is, by contract, yours to perform, and the platform attaches nothing to help. An EU region keeps the data in Europe; it does not mark a single output.

Signunder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Baseten's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. Signunder is not a law firm and this is not legal advice.*
