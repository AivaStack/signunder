---
title: "Does Modal sign your AI output? What its terms say"
description: "Modal attaches no C2PA manifest and no watermark. Its terms make you responsible for output. Read 12 October 2026."
slug: modal
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [modal, article-50, platforms, c2pa]
sources:
  - title: "Modal Software as a Service Agreement"
    url: https://modal.com/legal/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Modal add Content Credentials to generated images?"
    a: "No. Modal runs the containers you deploy and attaches nothing to what they produce, as read on 12 October 2026. Its agreement makes you solely responsible for output and for compliance with applicable law."
  - q: "Who is responsible for Article 50(2) marking when I generate on Modal?"
    a: "You are: you deploy the model, you are the provider of the system around it, and Modal's agreement puts output and legal compliance on you."
  - q: "Does Modal offer EU-region processing?"
    a: "Yes, EU regions are documented, with some metadata held in the US, as read on 8 October 2026."
related:
  - /platforms
  - /faq
  - /checker
draft: false
---

- Marks output: **No**; you run the model yourself.
- Watermark: **none**.
- Terms: "solely responsible for reviewing, validating, editing, and amending any Output"; compliance with all applicable laws.
- EU region: yes.

## Does Modal attach a C2PA manifest or a watermark?

Modal is infrastructure: you deploy the model, so there is no platform layer that could mark output on your behalf. Nothing in its documentation mentions Content Credentials, C2PA or watermarks, as searched on 12 October 2026. The agreement is dated "Effective May 2026"; the day is not given.

## What do Modal's terms say about who is responsible?

The terms were last updated on 1 May 2026. Two clauses matter here:

> "Customer is solely responsible for reviewing, validating, editing, and amending any Output" — Modal Software as a Service Agreement, Section 5.5, Use of Artificial Intelligence, read 12 October 2026

> "Customer shall comply with all applicable laws and regulations in its use of any Service" — Modal Software as a Service Agreement, Section 8.2, Compliance with Laws, read 12 October 2026

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what the system generates (our reading of 8 October 2026, shared by the law-firm notes we have seen). Modal's terms point the same way. So if your users see images, audio or video that Modal generated for you, the marking obligation is yours, and it is not discharged by anything Modal attaches.

## What this means if you generate through Modal

Self-hosting on Modal makes the position simple and entirely yours: the model is unmarked unless its weights were trained to watermark, and nobody between you and your users adds anything. The record has to be yours.

SignUnder is built to sit in front of APIs like this one and sign every output under your own name, with an invisible watermark and a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published.

*Read from Modal's own terms and documentation on 12 October 2026, and its region pages on 8 October 2026. Platforms change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
