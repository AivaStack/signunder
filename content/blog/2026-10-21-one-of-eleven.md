---
title: "One of eleven: which inference platforms mark AI output"
description: "We read the terms and documentation of eleven platforms hosting open-weight models. One attaches Content Credentials, signed as itself. All eleven put output on the customer."
slug: one-of-eleven
type: post
date: 2026-10-21
updated: 2026-10-21
reviewed: 2026-10-12
authors: [eoin]
tags: [platforms, article-50, c2pa]
sources:
  - title: "The eleven platform pages, each with its terms and documentation read on 12 October 2026"
    url: https://signunder.com/platforms
    read: 2026-10-12
  - title: "European Commission, Guidelines on Article 50 (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
faq:
  - q: "Which inference platforms sign C2PA Content Credentials?"
    a: "Of fal, Replicate, Together, Runware, Fireworks, Baseten, Hugging Face Inference, Modal, Novita, WaveSpeed and DeepInfra, only fal does, for media from its hosted applications, signed as fal."
  - q: "Does that mean the platforms are breaking the law?"
    a: "No. Article 50(2) binds the provider of the AI system, and the Commission's guidelines treat the business offering generation under its own name as that provider. The platforms' terms say the same: output is the customer's."
related:
  - /platforms
  - /law/eu-ai-act-article-50
  - /compare/fal
draft: true
---

Of eleven inference platforms that host open-weight image, video and audio models, one attaches Content Credentials to what it generates, and it signs them in its own name. The other ten attach nothing. All eleven, in their terms, make the customer responsible for output. We read every set of terms on 12 October 2026 and published a page per platform with the clause quoted; this is what the eleven say together.

## The count

fal.ai signs media generated through its hosted applications and watermarks it; whether its raw API output is signed is undocumented. Replicate, Together, Runware, Fireworks, Baseten, Hugging Face Inference Providers, Modal, Novita, WaveSpeed and DeepInfra document no manifest and no watermark. WaveSpeed sells a watermark remover. The [full table](/platforms) has each platform's terms date, EU region and the clause.

## What the terms say

Every one of the eleven places output on the customer, in language that ranges from general to precise. Replicate: "CUSTOMER IS ULTIMATELY RESPONSIBLE FOR ITS USE OF ANY MODELS OR OUTPUT". Together: "You shall be responsible for all of Your Content and Output". Runware: users are "solely responsible for ensuring that their inputs and the use of AI-generated content comply". Baseten uses the AI Act's own words: the customer "assumes and shall perform all obligations imposed on the developer, provider, and/or deployer of AI systems". Four of the eleven updated their terms between 8 September and 29 September 2026; none of the updates added marking.

## Why that is consistent with the law

The Commission's guidelines on Article 50, adopted on 20 July 2026, give the example of "a company [that] provides a generative or interactive AI application ... on the Union market under its own name or trademark" and say "The company is a provider responsible for compliance with the transparency obligations in Article 50(1) and/or (2)", whoever built the model. The platforms are reading the same text from the other side. Nobody is breaking anything; the obligation simply sits with the company whose users see the output, and the platforms have written down that they will not carry it.

## What a company shipping on these platforms is left with

Unmarked output from ten of them; a supplier's mark from the eleventh. Either way the marking the provider must do is its own: a manifest in its own name, a watermark for when the manifest is stripped, and a detection endpoint for its own outputs. The [platform pages](/platforms) say what each one does; the [own-name guide](/guides/sign-under-your-own-name) says why the name on the signature cannot be delegated.

*Not legal advice. Every quotation is from the platform's own terms on the date shown on its page.*
