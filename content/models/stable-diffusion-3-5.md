---
title: "Does Stable Diffusion 3.5 mark its output? Code and licence"
description: "SD 3.5's reference code has no watermark or C2PA step and its licence is silent on labelling. Marking is the deployer's job. Read 13 Oct 2026."
slug: stable-diffusion-3-5
type: model
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [stable-diffusion, stability-ai, models, c2pa, watermark, self-hosting]
sources:
  - title: "Stability-AI/sd3.5 repository: README and inference code (last push 8 January 2025)"
    url: https://github.com/Stability-AI/sd3.5
    read: 2026-10-13
  - title: "Stable Diffusion 3.5 Large model card on Hugging Face"
    url: https://huggingface.co/stabilityai/stable-diffusion-3.5-large
    read: 2026-10-13
  - title: "Stability AI Community License Agreement, last updated 5 July 2024"
    url: https://stability.ai/community-license-agreement
    read: 2026-10-13
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does Stable Diffusion 3.5 add a watermark?"
    a: "No. As read on 13 October 2026, the reference inference repository contains no watermarking code and its README and the model card do not mention watermarking, C2PA or provenance."
  - q: "Does the Stability AI Community License require labelling of output?"
    a: "Not in terms. It requires that use of outputs comply with applicable laws and regulations and makes the user solely responsible for the lawfulness of using them; it does not mention labelling, watermarking or the AI Act."
  - q: "Who is responsible for Article 50 marking with self-hosted SD 3.5?"
    a: "The business that offers the generation to users. The Commission's guidelines treat it as the provider of that AI system, and nothing in the model or its licence does the marking for it."
related:
  - /models
  - /platforms
  - /guides/sign-under-your-own-name
draft: false
---

- C2PA manifest: **none** from the open code.
- Watermark: **none**; the reference repository has no watermarking code.
- Licence: output must "comply with applicable laws and regulations"; **silent on labelling**.
- Free under **USD 1,000,000** in annual revenue; an enterprise licence above it.

## Does SD 3.5 attach a C2PA manifest or a watermark?

No. A search of every file in Stability AI's reference repository on 13 October 2026 found no watermark, C2PA, provenance or metadata-labelling code, and the README does not mention any of them. The repository holds about a dozen files and was last pushed on 8 January 2025. The model card for Stable Diffusion 3.5 Large is silent on the subject too; its safety section tells developers to add "content safety guardrails" suited to their use and says nothing about marking.

What Stability AI's own hosted API attaches to images is a separate question that this page does not answer: UNVERIFIED.

## What does the licence say?

The weights are under the Stability AI Community License Agreement, last updated 5 July 2024. Three points matter here (read 13 October 2026):

- **Ownership.** "You own any outputs generated from the Models or Derivative Works to the extent permitted by applicable law."
- **Responsibility.** The user is "SOLELY RESPONSIBLE FOR DETERMINING THE APPROPRIATENESS OR LAWFULNESS OF USING" the materials and outputs.
- **Law.** Use of outputs "must comply with applicable laws and regulations".

The agreement does not mention the EU AI Act, disclosure, labelling, watermarking or provenance. It is free for research, non-commercial and commercial use for those under USD 1,000,000 in annual revenue; above that the licence ends and an enterprise licence is needed. Commercial users must register with Stability AI.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what it generates (our reading of 8 October 2026). The licence leaves lawfulness with you and the model contributes no mark, so every obligation under Article 50(2) falls to the business that puts SD 3.5 in front of users.

## What this means if you run SD 3.5 yourself

Every image leaves your pipeline unmarked unless you mark it. That is true on your own GPUs and on the [inference platforms](/platforms) that host the model without marking. If your users are in the EU and your system was on the market before 2 August 2026, the grace period for marking ends on 2 December 2026.

SignUnder signs each output under your own name, adds an invisible watermark where the model added none, and gives you a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-13 — First published.

*Read from the model's own repository, licence and model card on 13 October 2026. Repositories change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
