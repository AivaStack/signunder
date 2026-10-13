---
title: "China's AI content labelling measures: visible and metadata labels"
description: "Since 1 September 2025 China requires a visible label and a metadata label on AI-generated content, and platforms must check for them. Read 13 Oct 2026."
slug: china
type: law
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [china, cac, gb-45438, law, labelling]
sources:
  - title: "Measures for the Labelling of AI-Generated and Synthetic Content (人工智能生成合成内容标识办法), notice 国信办通字〔2025〕2号 dated 7 March 2025, published by the Cyberspace Administration of China on 14 March 2025; Chinese text, our translation"
    url: https://www.cac.gov.cn/2025-03/14/c_1743654684782215.htm
    read: 2026-10-12
  - title: "Cyberspace Administration of China, announcement of the Measures and of the mandatory national standard that accompanies them, 14 March 2025"
    url: https://www.cac.gov.cn/2025-03/14/c_1743654685899683.htm
    read: 2026-10-12
  - title: "Cyberspace Administration of China, notice of action against the Jianying and Maoxiang apps and the Jimeng AI site over labelling, 28 April 2026"
    url: https://www.cac.gov.cn/2026-04/28/c_1779119736411711.htm
    read: 2026-10-13
  - title: "IT Home's reprint of the 28 April 2026 notice, used for the verbatim Chinese wording"
    url: https://www.ithome.com/0/944/539.htm
    read: 2026-10-12
  - title: "Cyberspace Administration of China, notice on false information spread without AI labels, 12 February 2026"
    url: https://www.cac.gov.cn/2026-02/12/c_1772636033171974.htm
    read: 2026-10-13
  - title: "TC260 secretariat notice of 23 June 2025 on six draft practice guides under GB 45438-2025, as reprinted by Secrss"
    url: https://www.secrss.com/articles/80110
    read: 2026-10-12
  - title: "Xinhua, report on the Measures taking effect, 2 September 2025 (secondary, for the standard's metadata field)"
    url: https://www.news.cn/digital/20250902/d08b592223954340af8c02ac3d7161e1/c.html
    read: 2026-10-12
faq:
  - q: "When did China's AI labelling rules take effect?"
    a: "On 1 September 2025 (Article 14 of the Measures, read 12 October 2026). The mandatory national standard that accompanies them, GB 45438-2025, took effect the same day."
  - q: "What is the difference between an explicit and an implicit label?"
    a: "An explicit label is one a user can plainly perceive, as text, sound or a graphic, in the content or the interface. An implicit label is added to the file's data by technical means and is not readily perceived by the user; the Measures require it in the file metadata (Articles 3 and 5)."
  - q: "Is an invisible watermark required in China?"
    a: "No. Article 5 requires the implicit label in the file metadata and only encourages providers to add digital watermarks as a further form of implicit label."
  - q: "Can a user get AI output without the visible label?"
    a: "Yes, on request. Article 9 lets a provider supply content without the explicit label once the user agreement has set out the user's labelling obligations, and the provider must keep the related logs for at least six months. The metadata label is not waived."
  - q: "What is the penalty?"
    a: "The Measures set no fine of their own. Article 13 sends violations to the cyberspace, telecommunications, public security and broadcasting authorities to handle under existing laws and regulations. The action of 28 April 2026 used summonses, correction orders and warnings."
related:
  - /law/eu-ai-act-article-50
  - /law/california-sb-942-ab-853
  - /guides/durable-content-credentials
draft: false
---

China has required two labels on AI-generated content since 1 September 2025: an explicit one that a person can see or hear, and an implicit one written into the file's metadata. Platforms that distribute content must check uploads for the metadata label and tell viewers what they found. The rules are the Measures for the Labelling of AI-Generated and Synthetic Content, issued by the Cyberspace Administration of China (CAC) with three other ministries, and a mandatory national standard, GB 45438-2025, that fixes the technical form. The CAC named three ByteDance products for non-compliance on 28 April 2026.

- In force: **1 September 2025** (Article 14).
- Two labels: an **explicit** label a user can perceive, and an **implicit** label in the **file metadata** carrying the content's AI attribute, the provider's name or code, and a content number.
- Watermarks are **encouraged, not required** (Article 5).
- Platforms must **verify the metadata** on upload and label content as generated, possibly generated or suspected generated (Article 6).
- Deleting, altering, forging or hiding a label is prohibited, and so is supplying tools to do it (Article 10).

The text is Chinese; the translations here are ours, made from the CAC's published text on 12 October 2026, and the article numbers are given so each can be checked against the original.

## Who does it apply to?

Article 2 applies the Measures to "network information service providers" that fall under three earlier regulations: the algorithm recommendation provisions, the deep synthesis provisions and the interim measures on generative AI services. Those regulations cover services offered to the public in mainland China (*from memory*; the three texts were not re-read for this page). A company that serves no users in China is outside them. A company that does is the "service provider" whether it trained the model or calls someone else's.

Three other groups have duties: platforms that distribute content (Article 6), app stores, which must ask at review whether an app offers generation and check its labelling materials (Article 7), and users, who must declare generated content when they publish it and use the labelling function the platform provides (Article 10).

## What must a provider that generates content do?

**Add an explicit label** where the service is one of those listed in Article 17(1) of the deep synthesis provisions, which covers services whose output could confuse or mislead the public (*from memory*). Article 4 sets the placement by medium: for text, a text or symbol notice at the start, the end or a suitable point in the middle, or a prominent notice in the interface; for audio, a spoken or rhythmic cue at the start, end or middle; for images, a prominent label at a suitable position; for video, a prominent label on the opening frame and around the player. When the service lets users download, copy or export the content, the provider must "ensure the file contains an explicit label that meets the requirements".

**Add an implicit label to the file metadata.** Article 5 requires it to contain "production element information such as the attribute of the generated content, the service provider's name or code, and a content number". The same article says providers are "encouraged" to add implicit labels in the form of digital watermarks. Metadata is defined as descriptive information embedded in the file header in a specific encoding format.

**Say how in the user agreement.** Article 8 requires the agreement to state the method and style of the labels and to prompt users to read the labelling requirements.

**Keep logs if the visible label is removed on request.** Under Article 9 a user may ask for content without the explicit label. The provider may supply it after the user agreement has made the user's labelling obligations and responsibility clear, and must keep the relevant logs, including who received the content, for at least six months.

## What must a platform that distributes content do?

Article 6 sets three cases, each ending in a prominent notice next to the published content:

1. The metadata carries an implicit label marking the content as generated: the platform tells the public it **is** generated content.
2. No implicit label is found, but the user declares the content generated: the notice says it **may be** generated.
3. No implicit label and no declaration, but the platform detects an explicit label or other traces of generation: it is treated as **suspected** generated content and labelled so.

In all three the platform adds its own "distribution element information" to the metadata: the content's attribute, the platform's name or code, and a content number. It must also offer users a labelling function and remind them to declare.

## What does the national standard add?

The CAC's announcement of 14 March 2025 says the mandatory national standard "Cybersecurity technology: labelling method for AI-generated and synthetic content" was approved by the State Administration for Market Regulation and the Standardization Administration (Announcement No. 3 of 2025) and takes effect with the Measures on 1 September 2025. Its number, GB 45438-2025, is given in the TC260 secretariat's notice of 23 June 2025, which put six practice guides out for comment: metadata labelling for text, image, audio and video files, protection of the labels, and detection.

We have not read the standard itself. Xinhua's report of 2 September 2025 says the metadata field name must contain "AIGC" and that the metadata covers whether the content is generated, the generating provider, the distributing platform and a unique number. The exact field layout, and whether a signed C2PA manifest carrying the same facts would be accepted in its place, are UNVERIFIED until we read GB 45438-2025 and the practice guides.

## What happens if you don't?

The Measures carry no penalty schedule. Article 13 says violations are handled by the cyberspace, telecommunications, public security and broadcasting authorities "according to their duties, under the relevant laws, administrative regulations and departmental rules".

Two actions show what that means in practice:

- **28 April 2026.** The CAC said the Jianying and Maoxiang apps and the Jimeng AI website, all ByteDance products, had "failed to effectively implement the labelling requirements for AI-generated and synthetic content", in breach of the Cybersecurity Law, the interim measures on generative AI services and the labelling Measures. Local cyberspace offices were directed to summon the operators, order corrections, issue warnings and deal strictly with the people responsible. The notice gives no fine (CAC notice, read 13 October 2026; verbatim wording from IT Home's reprint).
- **12 February 2026.** The CAC reported that platforms had dealt with 13,421 accounts and removed more than 543,000 items in a sweep against false information spread without AI labels, and that shops selling tutorials and software for removing labels were taken down (CAC notice, read 13 October 2026).

## How does it compare with Article 50?

| | China's labelling Measures | EU AI Act Article 50(2) |
|---|---|---|
| Since | 1 September 2025 | 2 August 2026; 2 December 2026 for systems on the market before then |
| Visible label | Required for listed services, placement set by medium | Not required of providers; deployers disclose deepfakes under 50(4) |
| Machine-readable mark | Metadata label, fields set by a mandatory standard | Machine-readable and detectable; technique left to the provider and the Code of Practice |
| Watermark | Encouraged | The Code of Practice pairs signed metadata with an imperceptible watermark |
| Platforms | Must verify metadata on upload and label three ways | No equivalent duty in Article 50 |
| Removing marks | Prohibited for everyone, including tool suppliers | The Code asks signatories to preserve marks |

The largest difference is the direction of the metadata. China specifies its own field and expects platforms to read it; the EU leaves the format open and the industry has settled on C2PA. A product serving both markets will write both.

## What this means if you ship AI generation

If your product is offered to users in mainland China, both labels have been due since 1 September 2025, and the metadata label has to follow the national standard, not only C2PA. If you export files, the visible label goes in the file unless the user has asked otherwise under your agreement and you keep the logs. If you serve no one in China, the Measures do not reach you, though content your users post to Chinese platforms will be checked for the metadata label and marked as suspected generated content if it shows other signs.

SignUnder signs C2PA manifests and adds a watermark; it does not write the GB 45438-2025 metadata field today, and this page will say so until it does.

## Changes

- 2026-10-13 — First published, from the CAC's text of the Measures, its announcement of the standard, and its notices of 12 February and 28 April 2026.

*SignUnder is not a law firm and this is not legal advice. The Chinese text is authoritative; translations are ours, and anything not read from a primary source is marked.*
