---
title: "The EU Code of Practice on AI-generated content: what it asks of you"
description: "Signed metadata plus an imperceptible watermark, text over 200 tokens included, and interoperable detection by 2 February 2027. Read 12 Oct 2026."
slug: eu-code-of-practice
type: law
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [eu-ai-act, code-of-practice, article-50, watermark, c2pa, detection]
sources:
  - title: "Code of Practice on Transparency of AI-Generated Content, final version (PDF, 38 pages), published 10 June 2026"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/129555
    read: 2026-10-12
  - title: "European Commission, Code of Practice on Transparency of AI-generated Content (policy page with timeline)"
    url: https://digital-strategy.ec.europa.eu/en/policies/code-practice-ai-generated-content
    read: 2026-10-12
  - title: "European Commission, Strong backing for the Code of Practice (signatory list), 31 July 2026, updated 24 September 2026"
    url: https://digital-strategy.ec.europa.eu/en/news/strong-backing-code-practice-transparency-ai-generated-content
    read: 2026-10-12
  - title: "European Commission, FAQ: Signing the Code of Practice on Transparency of AI-generated Content"
    url: https://digital-strategy.ec.europa.eu/en/faqs/signing-code-practice-transparency-ai-generated-content
    read: 2026-10-12
  - title: "European Commission, Guidelines on the transparency obligations under Article 50 (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
faq:
  - q: "Is the Code binding?"
    a: "No. Signing is voluntary, and the Commission's FAQ says not signing is not itself non-compliance with the AI Act. A non-signatory still has to meet Article 50 and show its market surveillance authority that its own measures are adequate. The Digital Omnibus also made clear the Code grants no presumption of conformity."
  - q: "Does the Code require C2PA?"
    a: "It never names C2PA. Sub-measure 1.1.1 asks for digitally signed, time-stamped metadata recording that content is AI-generated, in formats that support metadata. Signed metadata under an open standard is what the Code describes, and C2PA Content Credentials are the established one; the Code itself says relevant interoperability standards exist for digitally signed metadata and not yet for other techniques."
  - q: "Does text need a watermark?"
    a: "Free-form text longer than 200 tokens does, under Sub-measure 1.1.2; very short text, understood as shorter than 200 tokens, is excepted, and the Code expects that threshold to fall as methods improve."
  - q: "What is due on 2 February 2027?"
    a: "An interoperability solution for detection. Signatories will implement, by 2 February 2027, either a publicly available interoperable access method that routes detection queries to the relevant mechanism, or a publicly readable signpost in the content that tells the public which detection solution to use."
  - q: "Which inference platforms signed?"
    a: "None of the eleven we track: fal, Replicate, Together, Runware, Fireworks, Baseten, Hugging Face, Modal, Novita, WaveSpeed and DeepInfra do not appear in either section of the list as updated on 24 September 2026. Model providers Black Forest Labs and Bria do appear in Section 1."
related:
  - /law/eu-ai-act-article-50
  - /platforms
  - /guides/sign-under-your-own-name
draft: false
---

The Code of Practice on Transparency of AI-Generated Content is the Commission's voluntary route to complying with Article 50 of the AI Act. For a company that generates images, audio, video or text, Section 1 of the Code comes down to five things: sign metadata on every output, add an imperceptible watermark, make a detection solution available, do not strip other people's marks, and have an interoperable detection mechanism in place by 2 February 2027. The final text was published on 10 June 2026 and found adequate by the Commission and the AI Board in July 2026.

- Final Code: **10 June 2026**, 38 pages; Section 1 for providers (Article 50(2)), Section 2 for deployers (Article 50(4)).
- Marking: **digitally signed, time-stamped metadata** (Sub-measure 1.1.1) **plus an imperceptible watermark** (Sub-measure 1.1.2), because "no single marking technique suffices".
- Text: watermark required for free-form text **longer than 200 tokens**.
- Detection: a detection solution available in the Union (Measure 2.1); **interoperable detection by 2 February 2027** (Measure 3.4).
- Signatories as of 24 September 2026: **95 in Section 1, 192 in Section 2**; no inference platform among them.

## Is the Code law?

No. The Commission's FAQ on signing says plainly: "Signing the code of practice is voluntary" and "Not signing it does not constitute non-compliance with the AI Act". What signing buys is a recognised way to demonstrate compliance across all Member States; what not signing costs is having to show each market surveillance authority that your own measures are adequate, with more information requests along the way (FAQ, read 12 October 2026). The Digital Omnibus removed the Commission's power to turn the Code into binding common rules and states that codes under Article 50(7) "do not grant a presumption of conformity" (Regulation (EU) 2026/1744, recitals, read 12 October 2026).

Sections can be signed separately; individual commitments cannot. Model providers and marking-technology vendors may sign Section 1 even though Article 50(2) does not bind them directly.

## What does Commitment 1 require: marking?

The Code opens from a technical premise: "no single marking technique suffices to meet the four requirements in Article 50(2) AI Act, namely effectiveness, interoperability, robustness, and reliability", so "only an appropriate combination of marking techniques and associated detection mechanisms" can satisfy them (Code, recitals, read 12 October 2026). Measure 1.1 then asks for two layers.

**Sub-measure 1.1.1, digitally signed metadata.** "If content is generated, manipulated or exported in a data format that supports attaching metadata (e.g., an audio, image, video, or containerised text), Signatories will record information in the metadata on whether the content is AI-generated or manipulated. All recorded information will be digitally signed and time-stamped". The Code does not name a standard; it notes that for digitally signed metadata, unlike other techniques, interoperability standards already exist. C2PA Content Credentials are that standard in practice.

**Sub-measure 1.1.2, imperceptible watermarking.** "Signatories will ensure that AI-generated or manipulated content is marked with an imperceptible watermark, with the exception of very short text. For free-form text longer than 200 tokens, watermarking still needs to be applied, even though it may have lower reliability". "Very short text" is text shorter than 200 tokens, a threshold the Code expects to decrease. The watermark "is intended to serve as a robust mechanism to complement the digitally signed metadata", and may be applied after generation or built into the model.

Measure 1.2 covers non-removal: signatories "will make best efforts to preserve metadata markings on input data and content generated or manipulated by their AI system". Measures 1.3 and 1.4 are optional: richer provenance information and a function for perceptible marks.

## What does Commitment 2 require: detection?

Measure 2.1: "Signatories will make available a detection solution, composed of one or more detection mechanisms, to enable deployers, users of their generative AI system, third-party integrators, end-users exposed to the content, and other legitimate parties ... to verify whether content has been generated or manipulated by their AI system". It can be provided as a public specification, as software, or as "a cloud-based service accessible to users in the Union through an API" (Sub-measure 2.1.1, read 12 October 2026). The duty is first-party: to detect your own system's output, not to judge anyone else's.

## What is due by 2 February 2027?

Interoperability is staged, because "relevant interoperability standards and/or best practices are yet to be developed, except for digitally signed metadata". From 2 August 2026 signatories adopt established standards for metadata marking and detection, and publish how to access detection for their other techniques. Then:

> "Signatories will implement an interoperability solution for their detection mechanisms by 2 February 2027 by implementing one or more of the following: i) a publicly available interoperable industry standard access method that enables detection queries to be routed to relevant detection mechanisms ... ii) a publicly readable signpost or other interoperable mechanism in the AI-generated or manipulated content that will signal to the public which detection [solution to use]" — Code, Measure 3.4, read 12 October 2026 (final words completed by us across a page break)

A C2PA manifest that carries the watermark's identifier as a soft binding is one way to be the signpost.

## Who has signed?

The Commission's first list, published on 31 July 2026 and updated on 24 September 2026, names about 190 organisations: 95 in Section 1 and 192 in Section 2, with roughly half described as small and recent companies. Section 1 examples the Commission gives are Aleph Alpha, Anthropic, Black Forest Labs, Cohere, Google, Meta, Microsoft, Mistral, OpenAI and Synthesia. None of the eleven inference platforms on our [platform pages](/platforms) appears in either section (list read 12 October 2026). The list stays open for signature.

## What this means if you ship AI generation

If you sign, Section 1 is a build list: signed metadata and a watermark on every output, a public detection endpoint for your own outputs, preservation of upstream marks, and an interoperable detection mechanism by 2 February 2027. If you do not sign, Article 50(2) still applies and the Code is the clearest statement of what an authority will regard as adequate. Either way, the marks have to be yours: a mark in a supplier's name says the supplier generated it.

SignUnder implements Section 1's layers under your own name, with a per-customer detection endpoint designed to follow the interoperability mechanism when it is specified; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from the final Code text, the Commission's FAQ and the signatory list as updated 24 September 2026.

*SignUnder is not a law firm and this is not legal advice. Every measure here cites the Code with the date we read it.*
