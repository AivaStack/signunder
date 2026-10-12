---
title: "Does Replicate sign your AI output? What its terms say"
description: "Replicate attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: replicate
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [replicate, article-50, platforms, c2pa]
sources:
  - title: "Replicate Terms of Service"
    url: https://replicate.com/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Replicate add Content Credentials to generated images?"
    a: "No. As read on 12 October 2026, Replicate attaches no C2PA manifest and no watermark of its own, and its terms make the customer responsible for models and output."
  - q: "Who is responsible for Article 50(2) marking when I generate through Replicate?"
    a: "You are, if you integrate the model into your own user-facing product: the Commission's guidelines treat you as the provider of that AI system, and Replicate's terms place responsibility for output on you."
  - q: "Does Replicate offer EU-region processing?"
    a: "Not that its pages state, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**.
- Watermark: **none** from the platform; some models carry their own `apply_watermark` flag.
- Terms: "customer is ultimately responsible for its use of any models or output".
- EU region: none stated.

## Does Replicate attach a C2PA manifest or a watermark?

Nothing in Replicate's documentation mentions Content Credentials, C2PA or output marking, as searched on 12 October 2026. Some model listings expose an `apply_watermark` input inherited from the model's own code, which is the model author's invisible watermark, not a signed manifest and not something Replicate attaches or verifies (seen on model pages via search; UNVERIFIED on a Replicate documentation page).

## What do Replicate's terms say about who is responsible?

The terms were last updated on 1 April 2026. Two clauses matter here:

> "CUSTOMER IS ULTIMATELY RESPONSIBLE FOR ITS USE OF ANY MODELS OR OUTPUT" — Replicate Terms of Service, Section 8.9, Artificial Intelligence, read 12 October 2026

> "You are solely responsible for your Content, Community Models, and Customer Data" — Replicate Terms of Service, Section 3.1, Customer Controls, read 12 October 2026

The terms also prohibit "representing or misleading people into believing that the use of the Services or outputs are human-generated" (Section 2.7(d)(x)(e)). That is a rule about deception, not a marking requirement; a signed manifest in your own name is the surest way to comply with it.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Replicate's terms point the same way. So if your users see images, audio or video that Replicate generated for you, the marking obligation is yours, and it is not discharged by anything Replicate attaches.

## What this means if you generate through Replicate

Nothing arrives marked. Every image, clip or audio file you get back from Replicate is unmarked, and the terms say what happens to it is yours to manage. If your users are in the EU and your system was on the market before 2 August 2026, the grace period for marking ends on 2 December 2026.

Signunder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Replicate's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. Signunder is not a law firm and this is not legal advice.*
