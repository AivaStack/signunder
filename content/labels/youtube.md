---
title: "What YouTube does with Content Credentials and altered-content labels"
description: "YouTube requires disclosure of realistic synthetic content, auto-labels uploads with C2PA metadata, and does not penalise disclosure. Read 12 Oct 2026."
slug: youtube
type: label
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [youtube, labels, c2pa, platforms]
sources:
  - title: "YouTube Help, Disclosing use of altered or synthetic content"
    url: https://support.google.com/youtube/answer/14328491
    read: 2026-10-12
  - title: "Google, How we're increasing transparency for gen AI content with the C2PA (17 September 2024)"
    url: https://blog.google/technology/ai/google-gen-ai-content-transparency-c2pa/
    read: 2026-10-12
faq:
  - q: "Does YouTube read C2PA Content Credentials?"
    a: "Yes. YouTube may automatically label content containing C2PA metadata as altered or synthetic, alongside content made with its own generative tools and content its systems detect. A label applied because of C2PA metadata cannot be changed by the creator."
  - q: "What must creators disclose?"
    a: "Realistic, meaningful AI use: making a real person appear to say or do something they did not, altering footage of a real event or place, generating a realistic scene that did not occur. Not required for non-realistic content or minor edits."
  - q: "Does disclosing AI content hurt a video?"
    a: "YouTube's page says disclosing AI content will not limit a video's audience or affect its eligibility to earn money. Consistently failing to disclose can lead to a label applied by YouTube, removal, or suspension from the Partner Program."
  - q: "Does a Content Credential survive YouTube's processing?"
    a: "YouTube re-encodes every upload, so the uploaded file's manifest is not what a viewer receives; YouTube reads it at upload. Whether an invisible watermark survives the re-encode is UNVERIFIED and in our survival suite."
related:
  - /labels
  - /law/eu-ai-act-article-50
  - /platforms
draft: false
---

YouTube requires creators to disclose realistic altered or synthetic content, applies the label itself when an upload carries C2PA metadata, and does not let a creator remove a label that C2PA metadata triggered. Disclosure carries no reach or monetisation penalty by YouTube's own account; failing to disclose does. Because YouTube re-encodes everything, the manifest matters at upload, and whether a watermark survives is a question we are measuring.

- Reads Content Credentials: **yes**; C2PA metadata is one of three triggers for an automatic label.
- Creator duty: **disclose realistic, meaningful AI use** in the upload flow's "AI use" question.
- Label placement: in the player for photorealistic content, in the expanded description otherwise.
- Consequences: no reach or revenue penalty for disclosing; labels, removal or Partner Program suspension for repeatedly not disclosing.
- Rejects unmarked content: **no**.

## What YouTube says it does

From its Help page, read 12 October 2026: "we require creators to disclose when they use AI to meaningfully alter or generate photorealistic content", which includes making a real person appear to say or do something they did not, altering footage of a real event or place, or generating a realistic scene that did not occur. Beauty filters, colour changes, captions and cloning one's own voice do not need disclosure. Creators answer the "AI use" question in YouTube Studio's Attributes section; for photorealistic content a label may appear in the player, otherwise in the expanded description. "Disclosing AI content won't limit a video's audience or impact its eligibility to earn money."

YouTube "may automatically label content that was made with YouTube's GenAI tools, content containing C2PA metadata, or content its internal systems detect as AI-generated or altered", and creators are notified in Studio. A wrong automatic label can usually be changed by answering "No", except that "content made with YouTube's AI tools, content with C2PA metadata, or content labeled after manual review cannot be adjusted". Creators who consistently do not disclose "may face a manually applied label or penalties, such as content removal or suspension from the YouTube Partner Program".

Google's C2PA post of 17 September 2024 said it was "exploring ways to relay C2PA information to viewers on YouTube when content is captured with a camera"; a "Captured with a camera" disclosure now exists as a separate Help article.

## What this means for your output

A manifest on an uploaded video gets it labelled on YouTube with no action from the creator and no penalty, which is the outcome the EU and California rules want. The manifest does not travel to viewers, because YouTube serves its own encodes; the information travels as YouTube's label. For anything a viewer downloads or screen-records, only a watermark or a fingerprint can connect it back to your record, and whether YouTube's re-encode preserves a watermark is in our survival suite.

## Changes

- 2026-10-12 — First published, from YouTube's Help page and Google's C2PA post.

*Read from YouTube's and Google's own pages on the date shown. Platform policies change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
