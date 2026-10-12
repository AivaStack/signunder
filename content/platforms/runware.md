---
title: "Does Runware sign your AI output? What its terms say"
description: "Runware attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: runware
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [runware, article-50, platforms, c2pa]
sources:
  - title: "Runware Terms of Service"
    url: https://runware.ai/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Runware add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Runware attaches no C2PA manifest and no watermark, and its terms make the user solely responsible for the compliant use of AI-generated content."
  - q: "Who is responsible for Article 50(2) marking when I generate through Runware?"
    a: "You are, if the model sits inside your own user-facing product. Runware's terms say so in as many words for the use of AI-generated content, and the Commission's guidelines treat the integrator as the provider."
  - q: "Does Runware offer EU-region processing?"
    a: "Yes. It announced EU pods on 4 August 2026, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none**.
- Terms: users "solely responsible" for the compliant use of AI-generated content; misrepresenting the origin of outputs is prohibited.
- EU region: yes.

## Does Runware attach a C2PA manifest or a watermark?

Nothing in Runware's documentation mentions Content Credentials, C2PA, watermarks or output marking, as searched on 12 October 2026.

## What do Runware's terms say about who is responsible?

The terms were last updated on 29 September 2026. Two clauses matter here:

> "Users are solely responsible for ensuring that their inputs and the use of AI-generated content comply" — Runware Terms of Service, Section 12, User Responsibility for Input and Ethical Use, read 12 October 2026

> "Your use of the Services will not violate any applicable law or regulation." — Runware Terms of Service, Section 5, User Representations, read 12 October 2026

The terms also prohibit "misrepresent the origin of Outputs" (Section 10, Prohibited Activities). That is a rule about deception, not a marking requirement; a signed manifest in your own name is the surest way to comply with it.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Runware's terms point the same way. So if your users see images, audio or video that Runware generated for you, the marking obligation is yours, and it is not discharged by anything Runware attaches.

## What this means if you generate through Runware

EU processing solves data residency, not marking. Runware's outputs are unmarked, and its terms are among the clearest of the eleven in putting the compliant use of AI-generated content on you. The prohibition on misrepresenting the origin of outputs cuts the other way too: a signed manifest in your name is the strongest statement of origin there is.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Runware's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
