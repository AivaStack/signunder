---
title: "HunyuanImage 3.0 in the EU: the licence excludes it"
description: "Tencent's HunyuanImage 3.0 licence does not apply in the EU, UK or South Korea, and the open code adds no watermark or C2PA. Read 13 Oct 2026."
slug: hunyuanimage
type: model
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [hunyuanimage, tencent, models, licence, c2pa, watermark, self-hosting]
sources:
  - title: "Tencent Hunyuan Community License Agreement for Tencent Hunyuan Image 3.0, release date 28 September 2025, with Exhibit A Acceptable Use Policy (last modified 5 November 2024)"
    url: https://github.com/Tencent-Hunyuan/HunyuanImage-3.0/blob/main/LICENSE
    read: 2026-10-13
  - title: "Tencent-Hunyuan/HunyuanImage-3.0 repository: README and inference code (last push 23 June 2026)"
    url: https://github.com/Tencent-Hunyuan/HunyuanImage-3.0
    read: 2026-10-13
  - title: "HunyuanImage-3.0 model card on Hugging Face"
    url: https://huggingface.co/tencent/HunyuanImage-3.0
    read: 2026-10-13
faq:
  - q: "Can I use HunyuanImage 3.0 in the EU?"
    a: "Not under its licence. The agreement opens by stating that it does not apply in the European Union, United Kingdom and South Korea, defines the Territory as the rest of the world, and says use outside the Territory is unlicensed and unauthorized (read 13 October 2026)."
  - q: "Does the exclusion cover output as well as the model?"
    a: "Yes. Section 5(c) says you must not use, reproduce, modify, distribute or display the Tencent Hunyuan Works, Output or results outside the Territory."
  - q: "Does HunyuanImage 3.0 add a watermark or Content Credentials?"
    a: "No. As read on 13 October 2026, the open repository contains no watermarking, C2PA or metadata-labelling code."
  - q: "Does the licence require AI disclosure?"
    a: "Its Acceptable Use Policy forbids placing generated information in any public context without expressly and conspicuously identifying that it is machine generated."
related:
  - /models
  - /law/south-korea
  - /law/eu-ai-act-article-50
draft: false
---

- Licence territory: **worldwide except the European Union, the United Kingdom and South Korea**.
- The exclusion covers **output**, not only the weights (section 5(c)).
- C2PA manifest and watermark: **none** from the open code.
- Acceptable use: public output must be **"expressly and conspicuously"** identified as machine generated.

## Can HunyuanImage 3.0 be used in the EU?

Not under the licence it ships with. The Tencent Hunyuan Community License Agreement for Hunyuan Image 3.0, release date 28 September 2025, begins:

> "THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA AND IS EXPRESSLY LIMITED TO THE TERRITORY, AS DEFINED BELOW." — licence preamble, read 13 October 2026

Section 1 defines the Territory as "the worldwide territory, excluding the territory of the European Union, United Kingdom and South Korea". Section 5(c) then says:

> "You must not use, reproduce, modify, distribute, or display the Tencent Hunyuan Works, Output or results of the Tencent Hunyuan Works outside the Territory. Any such use outside the Territory is unlicensed and unauthorized under this Agreement." — section 5(c)

Two consequences follow from the wording. A company in the EU has no licence to run the model. And a company outside the EU that runs it lawfully still may not distribute or display its output to users in the EU, the UK or South Korea, because output is named in the clause. The first item of the Acceptable Use Policy repeats the point: you agree not to use the model "Outside the Territory".

The licence does not say why those three are excluded. The EU and South Korea both have AI laws with [labelling](/law/eu-ai-act-article-50) [duties](/law/south-korea) in force; whether that is the reason is not something Tencent states.

## Does it attach a C2PA manifest or a watermark?

No. A search of every file in the repository on 13 October 2026 found no watermark, C2PA, provenance or metadata-labelling code, and the README and model card do not mention marking. What Tencent's own hosted service attaches is not covered here: UNVERIFIED.

## What does the licence say about disclosure and responsibility?

The Acceptable Use Policy in Exhibit A forbids using the model "To generate or disseminate information (including images, code, posts, articles), and place the information in any public context" without "expressly and conspicuously identifying that the information and/or content is machine generated" (item 12). That is a visible-disclosure term, binding wherever the licence applies.

Section 5(a) requires use to "comply with applicable laws and regulations" and requires you to pass the use restrictions on in any agreement governing your own distribution. The licence makes the user responsible for outputs and their subsequent uses, and section 5(b) forbids using output to improve any other AI model.

## What this means if you are choosing a model

If any of your users are in the EU, the UK or South Korea, HunyuanImage 3.0 is the one model of the five on these pages that the licence rules out before marking is even a question. Signing the output does not cure a missing licence. Outside those territories, the model arrives unmarked and the acceptable-use policy expects a conspicuous disclosure on anything made public.

For a product that serves Europe, the practical choice is among the other four: [FLUX](/models/flux), [Stable Diffusion 3.5](/models/stable-diffusion-3-5), [Wan](/models/wan) and [Qwen-Image](/models/qwen-image).

## Changes

- 2026-10-13 — First published.

*Read from the model's own repository, licence and model card on 13 October 2026. Repositories change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
