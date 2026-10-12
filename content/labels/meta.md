---
title: "What Meta does with Content Credentials and the AI Info label"
description: "Meta's AI Info label comes from C2PA and IPTC signals or self-disclosure; realistic AI video and audio must be labelled, images need not. Read 12 Oct 2026."
slug: meta
type: label
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [meta, facebook, instagram, threads, labels, c2pa, iptc]
sources:
  - title: "Meta Newsroom, Labeling AI-Generated Images on Facebook, Instagram and Threads (6 February 2024)"
    url: https://about.fb.com/news/2024/02/labeling-ai-generated-images-on-facebook-instagram-and-threads/
    read: 2026-10-12
  - title: "Meta Transparency Center, Labeling AI Content"
    url: https://transparency.meta.com/governance/tracking-impact/labeling-ai-content
    read: 2026-10-12
  - title: "Meta Help Center, How to identify AI content on Meta products"
    url: https://www.meta.com/help/artificial-intelligence/1783222608822690/
    read: 2026-10-12
faq:
  - q: "Does Meta read C2PA Content Credentials?"
    a: "Yes. Meta said in February 2024 it was building tools to identify the AI-generated information in the C2PA and IPTC standards so it could label images from the major generators, and its Transparency Center says labels are applied when it detects industry-standard AI image indicators or when people disclose."
  - q: "Do I have to label AI content on Facebook or Instagram?"
    a: "Meta may require an AI label when content has photorealistic video or realistic-sounding audio that was digitally created or altered, including with AI. The requirement does not apply to images, though images may still be labelled if Meta's systems detect AI. There may be penalties for content shared without a required label."
  - q: "Does a Content Credential survive upload to Facebook or Instagram?"
    a: "Meta reads it at upload to label the post. Whether the manifest remains in a downloaded or re-shared file is UNVERIFIED; metadata stripping on upload to Facebook is widely reported, and we are measuring it ourselves."
  - q: "Does Meta reject unmarked AI content?"
    a: "No. Since 2024 its approach is labelling rather than removal unless other policies are broken. Press reports of reach limits for unlabelled AI-persona accounts on Instagram from 31 August 2026 are UNVERIFIED against Meta's own pages."
related:
  - /labels
  - /law/eu-ai-act-article-50
  - /platforms
draft: false
---

Meta labels AI-generated content on Facebook, Instagram and Threads as "AI Info" when it detects industry-standard indicators, including the C2PA and IPTC metadata that a signed manifest carries, or when the poster discloses. A label is required of the poster for photorealistic video and realistic audio, not for images, and omissions may be penalised. Meta does not reject unmarked content. What survives inside the file after upload is a separate question that we are measuring.

- Reads Content Credentials: **yes**, C2PA and IPTC "AI generated" signals, since 2024.
- Label name: **"AI Info"** (renamed from "Made with AI" in 2024), applied from signals or self-disclosure.
- Poster's duty: **label photorealistic video and realistic audio**; images exempt from the duty but may be labelled by detection.
- Rejects unmarked content: **no**; penalties possible for missing required labels.
- Survival of marks in the file and display for untrusted signers: **UNVERIFIED**, being measured.

## What Meta says it does

From its newsroom post of 6 February 2024, read 12 October 2026: Meta was "building industry-leading tools that can identify invisible markers at scale – specifically, the 'AI generated' information in the C2PA and IPTC technical standards – so we can label images from Google, OpenAI, Microsoft, Adobe, Midjourney, and Shutterstock as they implement their plans". Its own Meta AI images carry "IPTC metadata and invisible watermarks", and "Using both invisible watermarking and metadata in this way improves both the robustness of these invisible markers and helps other platforms identify them."

The Transparency Center page, read the same day, says Meta began labelling in May 2024, that "These labels are now displayed as 'AI Info'", and that it added them "when we detected industry standard AI image indicators or when people disclosed that they were uploading AI-generated content". It reports over 360 million labelled pieces of content with user label views on Facebook alone in 2024.

The Help Center page sets the poster's duty: "Meta may require an AI label when content has photorealistic video or realistic-sounding audio that was digitally created, modified or altered, including with AI. This requirement does not apply to images. However, images may still receive a label if the systems detect that they were AI-generated or if they were modified using AI." And: "There may be penalties for content shared without a label when it is required."

## What this means for your output

A signed manifest and IPTC metadata on your images get them labelled "AI Info" on Meta's apps at upload, which is what the disclosure rules want. The two open questions are the same as everywhere: whether Meta shows anything about the signer, trusted or not, and whether the manifest and watermark are still in the file a viewer saves. IPTC has long reported that uploading to Facebook strips image metadata (secondary); if so, the watermark is the only layer that reaches the downloaded file, and the fingerprint the only one that survives a screenshot. Our survival suite measures all three and this page will carry the results.

## Changes

- 2026-10-12 — First published, from Meta's newsroom, Transparency Center and Help Center pages.

*Read from Meta's own pages on the date shown. Platform policies change; tell us if this is out of date. SignUnder is not a law firm and this is not legal advice.*
