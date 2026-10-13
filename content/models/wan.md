---
title: "Does Wan 2.2 mark its video output? What the code and licence say"
description: "Wan 2.2's open code has no watermark or C2PA step. Apache 2.0, and the README says you are fully accountable for use. Read 13 Oct 2026."
slug: wan
type: model
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [wan, alibaba, models, video, c2pa, watermark, self-hosting]
sources:
  - title: "Wan-Video/Wan2.2 repository: README, LICENSE.txt and inference code (last push 21 September 2026)"
    url: https://github.com/Wan-Video/Wan2.2
    read: 2026-10-13
  - title: "Wan2.2-T2V-A14B model card on Hugging Face"
    url: https://huggingface.co/Wan-AI/Wan2.2-T2V-A14B
    read: 2026-10-13
  - title: "SignUnder, China's AI content labelling measures (for the Measures' scope and Article 5)"
    url: https://signunder.com/law/china
    read: 2026-10-13
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Wan 2.2 watermark its videos?"
    a: "No. As read on 13 October 2026, the open repository contains no watermarking, C2PA or metadata-labelling code, and neither the README nor the model card mentions any."
  - q: "What licence is Wan 2.2 under?"
    a: "Apache 2.0. The README adds that the authors claim no rights over generated content and that you are fully accountable for your use of the models."
  - q: "Wan comes from a Chinese company. Do China's labelling rules mark the output?"
    a: "Not in the open weights. China's Measures bind services offered to the public in China, and the open code writes neither the visible label nor the metadata label those Measures require."
related:
  - /models
  - /law/china
  - /guides/sign-under-your-own-name
draft: false
---

- C2PA manifest: **none** from the open code.
- Watermark: **none**; nothing in the repository's 87 files.
- Licence: **Apache 2.0**; "You are fully accountable for your use of the models".
- Output: video and, for the speech-to-video model, audio-driven video. Video is the hardest medium to mark after the fact.

## Does Wan 2.2 attach a C2PA manifest or a watermark?

No. A search of every file in the Wan2.2 repository on 13 October 2026 found no watermark, C2PA, provenance or metadata-labelling code. The README, which covers the text-to-video, image-to-video, speech-to-video and animation models released between 28 July and 19 September 2025, does not mention marking, and nor does the model card.

Wan is developed by Alibaba, and China has required a visible label and a metadata label on generated content since 1 September 2025. Those [Measures](/law/china) bind services offered to the public in China, not model weights, and the open code writes neither label. What Alibaba's own hosted Wan service attaches is not covered here: UNVERIFIED.

## What does the licence say?

The models are under Apache 2.0, which has no terms about output. The README's licence section adds a statement of its own:

> "We claim no rights over the your generated contents, granting you the freedom to use them while ensuring that your usage complies with the provisions of this license. You are fully accountable for your use of the models" — Wan2.2 README, License Agreement, read 13 October 2026

It goes on to say use must not involve sharing content that violates applicable laws or "spreads misinformation". There is no labelling or disclosure term.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what it generates (our reading of 8 October 2026). With an Apache licence and no mark in the code, a product that offers Wan-generated video to users carries Article 50(2) in full, and there is no upstream manifest to preserve.

## What this means if you run Wan yourself

Every clip is unmarked when it leaves the model. Video needs both layers: a signed manifest in the container, which re-encoding on upload can remove, and a watermark in the frames to recover it. If your users are in the EU and your system was on the market before 2 August 2026, the grace period for marking ends on 2 December 2026.

SignUnder signs images and audio first; video signing comes after, and this page will say when it does. It is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-13 — First published.

*Read from the model's own repository, licence and model card on 13 October 2026. Repositories change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
