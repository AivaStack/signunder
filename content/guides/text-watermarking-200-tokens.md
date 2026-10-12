---
title: "The 200-token text rule in the EU Code of Practice"
description: "The Code expects an imperceptible watermark on free-form AI text longer than 200 tokens, exempts shorter text, and expects the threshold to fall. What it says and where the limits are. Read 12 Oct 2026."
slug: text-watermarking-200-tokens
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [text, watermark, code-of-practice, article-50]
sources:
  - title: "Code of Practice on Transparency of AI-Generated Content, Sub-measure 1.1.2 and the definition of very short text"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/129555
    read: 2026-10-12
  - title: "Regulation (EU) 2024/1689, Article 50(2)"
    url: https://eur-lex.europa.eu/eli/reg/2024/1689/oj
    read: 2026-10-12
faq:
  - q: "Does AI-generated text have to be watermarked?"
    a: "Under Article 50(2), text is among the outputs that must be marked in a machine-readable format. Under the Code of Practice, signatories watermark free-form text longer than 200 tokens imperceptibly, and are excused for very short text, understood as shorter than 200 tokens."
  - q: "Why 200 tokens?"
    a: "The Code says that at publication, state-of-the-art techniques enable watermarking with at least a basic level of reliability of text as short as 200 tokens, and expects the threshold to decrease as methods improve."
  - q: "Is 200 tokens the same as 200 words?"
    a: "No. A token is a model's unit of text, usually a word fragment; 200 tokens is roughly 150 English words, though it varies by language and tokeniser."
related:
  - /law/eu-code-of-practice
  - /law/eu-ai-act-article-50
draft: false
---

The EU Code of Practice on Transparency of AI-Generated Content asks signatories to watermark AI-generated text imperceptibly, with one exception: very short text, which it defines as shorter than 200 tokens. Text longer than that needs a watermark even though the Code accepts its reliability will be lower than for long text, and it expects the threshold to fall as techniques improve. Text is also the one output where metadata rarely travels, so the watermark carries most of the load.

- Article 50(2) covers **text** alongside audio, image and video.
- The Code: imperceptible watermark on free-form text **longer than 200 tokens**; shorter text excused.
- Reliability: accepted as **lower** for shorter text; detection access may compensate.
- The threshold is expected to **decrease**.

## What the Code says

Sub-measure 1.1.2, read 12 October 2026: "Signatories will ensure that AI-generated or manipulated content is marked with an imperceptible watermark, with the exception of very short text. For free-form text longer than 200 tokens, watermarking still needs to be applied, even though it may have lower reliability compared to that of watermarking very long text. To compensate for the potential lower reliability of watermarking for free-form text, access to the corresponding detection solution may" be made available in ways the Code goes on to describe.

The definition: "At the time of publication of the code, state-of-the-art techniques enable watermarking with at least a basic level of reliability of text as short as 200 tokens, with the expectation that this threshold will decrease as new methods become available. Until then, 'very short text' should be understood as text shorter than 200 tokens."

## Why text is different

An image file has somewhere to put a manifest; a string of text pasted into a form does not. The Code's Sub-measure 1.1.1 asks for signed metadata only where the format supports it, naming "containerised text" as a case that does. For text that leaves as text, the watermark is the mark: a statistical bias in word choice that a detector can recognise at the provider's detection endpoint, which is why the Code ties text watermarking to access to detection.

## What it means in practice

If your product generates free-form text for EU users and you follow the Code, outputs over 200 tokens need a text watermark at generation, from the model or from a post-hoc method, and a detection solution that can read it. Chat interfaces are also subject to Article 50(1), the disclosure that a person is talking to an AI, which is a separate duty covered on the [Article 50 tracker](/law/eu-ai-act-article-50).

SignUnder's text module covers the disclosure strings and the Code's text measures; text watermarking itself depends on the model, and we say so rather than imply otherwise. Pre-launch; waitlist on the homepage.

## Changes

- 2026-10-12 — First published, from the Code's text.

*Not legal advice. Quotations are from the Code on the date shown.*
