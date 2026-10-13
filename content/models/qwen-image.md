---
title: "Does Qwen-Image mark its output? What the code and licence say"
description: "Qwen-Image's open code has no watermark or C2PA step and its Apache 2.0 licence says nothing about output. Marking is yours. Read 13 Oct 2026."
slug: qwen-image
type: model
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [qwen-image, alibaba, models, c2pa, watermark, self-hosting]
sources:
  - title: "QwenLM/Qwen-Image repository: README, LICENSE and example code (last push 10 February 2026)"
    url: https://github.com/QwenLM/Qwen-Image
    read: 2026-10-13
  - title: "Qwen-Image model card on Hugging Face"
    url: https://huggingface.co/Qwen/Qwen-Image
    read: 2026-10-13
  - title: "SignUnder, China's AI content labelling measures (for the Measures' scope)"
    url: https://signunder.com/law/china
    read: 2026-10-13
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Qwen-Image add a watermark or Content Credentials?"
    a: "No. As read on 13 October 2026, the open repository contains no watermarking, C2PA or metadata-labelling code, and neither the README nor the model card mentions any."
  - q: "What licence is Qwen-Image under?"
    a: "Apache 2.0. The README's licence section is one sentence: Qwen-Image is licensed under Apache 2.0. Nothing in it concerns output, labelling or responsibility."
  - q: "Who has to mark Qwen-Image output under the EU AI Act?"
    a: "The business that offers the generation to users. The Commission's guidelines treat it as the provider of that AI system under Article 50(2)."
related:
  - /models
  - /law/china
  - /guides/sign-under-your-own-name
draft: false
---

- C2PA manifest: **none** from the open code.
- Watermark: **none**; nothing in the repository's 15 files.
- Licence: **Apache 2.0**, with no terms on output or labelling.
- The model is known for rendering text inside images, which makes unmarked output easier to mistake for a real document or sign.

## Does Qwen-Image attach a C2PA manifest or a watermark?

No. A search of every file in the Qwen-Image repository on 13 October 2026 found no watermark, C2PA, provenance or metadata-labelling code. The README and the Hugging Face model card describe generation and editing and do not mention marking.

Qwen-Image is developed by Alibaba. China's [labelling Measures](/law/china) require a visible label and a metadata label, but they bind services offered to the public in China, not open weights, and the open code writes neither. What Alibaba's own hosted services attach is not covered here: UNVERIFIED.

## What does the licence say?

"Qwen-Image is licensed under Apache 2.0." That sentence is the README's whole licence section (read 13 October 2026). Apache 2.0 governs the code and weights and has nothing to say about what you generate, who is responsible for it or how it must be labelled. Of the five models on these pages it is the most permissive and the least informative: there is no acceptable-use policy and no disclosure term to pass on to your own users.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what it generates (our reading of 8 October 2026). Nothing upstream shares the duty: no manifest arrives to preserve, and no licence term tells you to add one.

## What this means if you run Qwen-Image yourself

Every image is unmarked when it leaves the model, on your own GPUs and on the [inference platforms](/platforms) that host it without marking. If your users are in the EU and your system was on the market before 2 August 2026, the grace period for marking ends on 2 December 2026.

SignUnder signs each output under your own name, adds an invisible watermark where the model added none, and gives you a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-13 — First published.

*Read from the model's own repository, licence and model card on 13 October 2026. Repositories change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
