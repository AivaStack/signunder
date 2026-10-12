---
title: "Which AI inference platforms mark their output? Eleven checked"
description: "Eleven platforms hosting open-weight models, read 12 October 2026: one attaches Content Credentials, signed as itself; all put output on the customer."
type: platform
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [platforms, article-50, c2pa]
sources:
  - title: "Each platform's terms and documentation, linked from its own page"
    url: https://signunder.com/platforms
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Which inference platforms attach C2PA Content Credentials to generated images?"
    a: "Of the eleven checked on 12 October 2026, one: fal.ai, for media generated through its hosted applications, signed as fal. The other ten attach no manifest and no watermark."
  - q: "If my platform does not mark output, who has to?"
    a: "You do, if the model sits inside your own user-facing product. The Commission's guidelines of 20 July 2026 treat the integrator as the provider of the AI system, and every one of the eleven platforms' terms places responsibility for output on the customer."
  - q: "How was this table produced?"
    a: "By reading each platform's terms and documentation on the dates shown and quoting the clause that allocates responsibility. Anything we could not confirm from a primary page is marked UNVERIFIED."
related:
  - /faq
  - /checker
draft: false
---

Of eleven inference platforms that host open-weight image, video and audio models, **one** attaches Content Credentials to generated output, and it signs them in its own name. The other ten attach nothing, and all eleven place responsibility for output on the customer in their terms. Read from each platform's own pages on 12 October 2026.

| Platform | Marks output | Watermark | What the terms say | EU region | Terms dated |
|---|---|---|---|---|---|
| [fal.ai](/platforms/fal) | Yes, signed as fal (hosted apps) | Yes | "Customer must use the AI Features and the Output Content only (i) in a lawful manner" | None stated; US-based | 8 September 2026 |
| [Replicate](/platforms/replicate) | No | No | "CUSTOMER IS ULTIMATELY RESPONSIBLE FOR ITS USE OF ANY MODELS OR OUTPUT" | None stated | 1 April 2026 |
| [Together AI](/platforms/together) | No | No | "You shall be responsible for all of Your Content and Output, including ensuring that it…" | Dedicated endpoints only | 19 May 2026 |
| [Runware](/platforms/runware) | No | No | "Users are solely responsible for ensuring that their inputs and the use of AI-generated…" | Yes: EU pods, announced 4 Aug 2026 | 29 September 2026 |
| [Fireworks AI](/platforms/fireworks) | No | No | "You acknowledge and agree that you are entirely responsible for your Content, and for a…" | "Contact sales" for EU residency | 10 July 2026 |
| [Baseten](/platforms/baseten) | No | No | "Customer assumes and shall perform all obligations imposed on the developer, provider,…" | Yes: `eu` region | 15 September 2026 |
| [Hugging Face Inference Providers](/platforms/hugging-face) | No | No | "You are solely responsible for the Content you post, publish, display or otherwise make…" | UNVERIFIED | 15 September 2022 |
| [Modal](/platforms/modal) | No | No | "Customer is solely responsible for reviewing, validating, editing, and amending any Output" | Yes: EU regions; some metadata in the US | 1 May 2026 |
| [Novita AI](/platforms/novita) | No | No | "YOU ARE SOLELY RESPONSIBLE FOR EVALUATING AND VERIFYING ALL OUTPUT BEFORE RELYING UPON IT." | Germany GPU zones | 5 August 2026 |
| [WaveSpeedAI](/platforms/wavespeed) | No | Sells a remover | "Customer is responsible for their use of the Services, including any content generation…" | None stated | 28 September 2026 |
| [DeepInfra](/platforms/deepinfra) | No | No | "it is solely responsible for its own products, services, and end-user relationships, fo…" | None stated | 17 August 2026 |

## What "marks output" means

A platform marks output when it attaches a C2PA manifest, signed with its own certificate, to the file it returns. fal does, for its hosted applications, and a verifier shows fal as the signer. No platform among the eleven signs in the customer's name, and under the C2PA Certificate Policy none can: a signing certificate is issued only to a product on the conforming-products list, with a subject that matches the listing.

## Why the terms column matters

The Commission's guidelines on Article 50 of the AI Act, adopted on 20 July 2026, treat a business that integrates a model by API into its own user-facing system as the provider of that system (our reading of 8 October 2026). The terms say the same from the other side: each platform makes the customer responsible for output, and Baseten does so using the AI Act's own words. Both readings land in the same place. If your users see what these platforms generate for you, the machine-readable marking that Article 50(2) requires is yours to add.

## Method

Each page quotes the clause from the platform's current terms, with the section and the date read, and says whether the platform's documentation mentions Content Credentials, C2PA or watermarks. Region information was read on 8 October 2026. The reads are repeated when a platform changes its terms, and every change is logged on the page. To cite this table: SignUnder, "Which AI inference platforms mark their output?", signunder.com/platforms, as read on the date shown.

## Changes

- 2026-10-12 — First published, replacing the table on the homepage, which had been read on 8 October 2026.

*SignUnder is not a law firm and this is not legal advice.*
