---
title: "Does self-hosted FLUX mark its output? What the code and licence say"
description: "FLUX.2's reference code writes an EXIF tag and ships a watermark that is switched off. Only BFL's own API signs with C2PA. Read 13 Oct 2026."
slug: flux
type: model
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [flux, black-forest-labs, models, c2pa, watermark, self-hosting]
sources:
  - title: "black-forest-labs/flux2 repository: README, scripts/cli.py, src/flux2/watermark.py and model_cards/FLUX.2-dev.md (last push 12 March 2026)"
    url: https://github.com/black-forest-labs/flux2
    read: 2026-10-13
  - title: "FLUX [dev] Non-Commercial License v2.0, in the flux2 repository"
    url: https://github.com/black-forest-labs/flux2/blob/main/model_licenses/LICENSE-FLUX-DEV
    read: 2026-10-13
  - title: "black-forest-labs/flux repository (FLUX.1): src/flux/util.py (last push 31 July 2025)"
    url: https://github.com/black-forest-labs/flux
    read: 2026-10-13
  - title: "Black Forest Labs help centre, What is C2PA in FLUX-generated images?"
    url: https://help.bfl.ai/articles/3705384267-c2pa-in-flux-generated-images
    read: 2026-10-13
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 of the AI Act (20 July 2026), via the Commission's AI Act pages"
    url: https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai
    read: 2026-10-08
faq:
  - q: "Does self-hosted FLUX add Content Credentials?"
    a: "No. As read on 13 October 2026, Black Forest Labs attaches C2PA Content Credentials only to images from its own API and Playground, and says self-hosted deployments of open-weight FLUX models are outside its signing infrastructure."
  - q: "Does FLUX add an invisible watermark?"
    a: "The FLUX.2 reference code includes a watermark module, but the call to it in the command-line script is commented out. The FLUX.1 reference code does apply it. In both cases it is one fixed 48-bit message, the same for every user."
  - q: "What does the FLUX licence say about labelling output?"
    a: "The FLUX [dev] Non-Commercial License v2.0 lets you create output only if you ensure it includes disclosure that it was generated or modified using AI, to the extent required under applicable law (section 2(e)(ii))."
  - q: "Can I use Black Forest Labs' signing certificate?"
    a: "No. BFL's help centre says its signing certificates are not available for third-party use and that operators who need C2PA signing must run their own signing infrastructure."
related:
  - /models
  - /platforms
  - /guides/sign-under-your-own-name
draft: false
---

- C2PA manifest: **only from BFL's own API and Playground**, signed as Black Forest Labs.
- Self-hosted reference code: an **EXIF tag** reading "AI generated", and a watermark module that is **switched off** in FLUX.2.
- Licence: you must ensure output carries an AI disclosure **"to the extent required under applicable law"**.
- BFL's position: **"Deployers are responsible for applicable content-marking obligations."**

## Does FLUX attach a C2PA manifest?

Through Black Forest Labs' own service, yes. Its help centre says "Content Credentials are attached to all images generated through" the BFL API and the Developer Playground, and that BFL "Signs the manifest using BFL's X.509 signing certificate". The manifest names Black Forest Labs, the FLUX model and version, and the creation time, and records no prompt (read 13 October 2026).

For everything else, no. The same article says "BFL does not attach or validate Content Credentials for outputs produced by third-party inference environments", that self-hosted deployments of open-weight models "are outside BFL's signing infrastructure", and that "BFL signing certificates are not available for third-party use." Operators who need C2PA signing "must run their own signing infrastructure".

## What does the open code do on its own?

Two things, neither of them a signed manifest.

**An EXIF tag.** The FLUX.2 command-line script saves each image with the EXIF Software field set to "AI generated;flux2" and Make set to "Black Forest Labs" (`scripts/cli.py`, read 13 October 2026). EXIF is plain metadata: unsigned, and removed by most platforms and many editors.

**A watermark that is off.** The repository contains `src/flux2/watermark.py`, which embeds a fixed 48-bit message with the open-source invisible-watermark library, and the README says "We've added an option to embed invisible watermarks directly into the generated images". In the script, both the import and the call are commented out: `# x = embed_watermark(x)`. A self-hoster running the script as shipped gets no watermark. The older FLUX.1 repository does call it, in `src/flux/util.py`.

The message is the same constant for everyone, so where it is applied it says "made with the FLUX reference code", not who generated the image. How well it survives resizing and re-encoding is UNVERIFIED until our survival suite measures it. And the reference scripts are not how most people run FLUX: other runtimes such as ComfyUI and diffusers load the weights with their own code, which does not include this module (*from memory*; not re-read for this page).

The README adds: "we are recommending implementing a solution to mark the metadata of your outputs, such as C2PA".

## What does the licence require?

The FLUX [dev] Non-Commercial License v2.0 covers the [dev] weights. Section 2(e) lets you use the model or create output only if you filter or review for unlawful content and "(ii) ensure Output includes disclosure (or other indication) that the Output was generated or modified using artificial intelligence technologies to the extent required under applicable law." Section 2(d) says "You are solely responsible for the Outputs you generate and their subsequent uses". The licence's list of prohibited uses names the EU Artificial Intelligence Act among the laws you must not violate.

FLUX.2 [klein] 4B is under Apache 2.0, which says nothing on marking; commercial self-hosting of [dev] needs separate terms from BFL, which we have not read.

## So who is the provider under Article 50(2)?

The Commission's guidelines on Article 50, adopted on 20 July 2026, treat a business that integrates a model into its own user-facing system as the provider of that AI system, answerable under Article 50(2) for what it generates (our reading of 8 October 2026). BFL says the same in its own words: "Deployers are responsible for applicable content-marking obligations." If you run FLUX on your own GPUs or through a third-party host, marking is yours, and the licence makes it a condition of use where the law requires it.

## What this means if you run FLUX yourself

Out of the box you get an EXIF tag at most. To meet Article 50(2) you need a machine-readable mark that survives, which in practice means a signed manifest under a certificate of your own and a watermark you control.

SignUnder signs each output under your own name, adds an invisible watermark where the model added none, and gives you a public verification page; it is pre-launch, and the waitlist is on the homepage.

## Changes

- 2026-10-13 — First published.

*Read from the model's own repository, licence and model card on 13 October 2026. Repositories change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
