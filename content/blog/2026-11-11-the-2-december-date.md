---
title: "The 2 December date: what changes, and what does not"
description: "On 2 December 2026 the Article 50(2) marking grace ends for AI systems on the market before 2 August. Who it covers, and what stays the same."
slug: the-2-december-date
type: post
date: 2026-11-11
updated: 2026-11-11
reviewed: 2026-10-12
authors: [eoin]
tags: [eu-ai-act, article-50, deadlines]
sources:
  - title: "Regulation (EU) 2026/1744 of 8 July 2026 (the Digital Omnibus on AI), recital 38 and Article 111(4) as inserted"
    url: https://eur-lex.europa.eu/eli/reg/2026/1744/oj
    read: 2026-10-12
  - title: "Regulation (EU) 2024/1689 (the AI Act), Articles 50, 99 and 113"
    url: https://eur-lex.europa.eu/eli/reg/2024/1689/oj
    read: 2026-10-12
  - title: "European Commission, Guidelines on Article 50 (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
  - title: "European Commission, FAQ: Transparency obligations under Article 50 AI Act"
    url: https://digital-strategy.ec.europa.eu/en/faqs/transparency-obligations-under-article-50-ai-act
    read: 2026-10-12
faq:
  - q: "What happens on 2 December 2026 under the EU AI Act?"
    a: "The four-month transitional period for marking ends. Providers of generative AI systems placed on the market before 2 August 2026 must comply with Article 50(2) by that date, under Article 111(4) as inserted by Regulation (EU) 2026/1744."
  - q: "Does the 2 December date apply to a product launched after 2 August 2026?"
    a: "No. The transitional rule covers only systems placed on the market before 2 August 2026. A system launched on or after that date has had to mark its output from its first day."
  - q: "Do I have to mark content generated before the deadline?"
    a: "The Commission's FAQ says content generated and published before 2 August 2026 does not have to be marked retroactively."
related:
  - /law/eu-ai-act-article-50
  - /law/eu-code-of-practice
  - /platforms
draft: true
---

On 2 December 2026 the one transitional relief in Article 50 of the EU AI Act runs out. From that day, every generative AI system on the EU market must mark its output machine-readably, including those that were already on the market when the article began to apply. Nothing else about Article 50 changes on that date, and a good deal of what is said about it is wrong, so this post separates the two.

## What is the 2 December date?

Article 50 has applied since 2 August 2026. The Digital Omnibus, Regulation (EU) 2026/1744, left it in place and added one sentence, a new Article 111(4):

> "Providers of AI systems, including general-purpose AI systems, generating synthetic audio, image, video or text content, that have been placed on the market before 2 August 2026 shall take the necessary steps in order to comply with Article 50(2) by 2 December 2026." — Regulation (EU) 2026/1744, read 12 October 2026

Recital 38 calls it "a transitional period of four months for providers who have already placed their systems on the market". The Commission's guidelines call it "a targeted grandfathering rule only with regard to the marking and detection obligations under Article 50(2)".

## What changes on 2 December?

One thing. A provider whose generative system was on the market before 2 August 2026 loses the extra time. From 2 December its outputs must be "marked in a machine-readable format and detectable as artificially generated or manipulated", like everyone else's.

"Provider" here includes a business that offers generation under its own name on someone else's model. The guidelines give that example and say the company "is a provider responsible for compliance with the transparency obligations in Article 50(1) and/or (2)". If your product has offered image, audio, video or text generation to EU users since before August, this is your date.

## What does not change?

- **Systems launched on or after 2 August 2026.** They never had the grace. The duty has applied to them from their first day.
- **The disclosure duties.** The grace covers Article 50(2) only. The guidelines say a system that is partly interactive and partly generative gets the extra time for marking and not for the Article 50(1) duty to tell people they are dealing with AI, which applied from 2 August.
- **Old content.** The Commission's FAQ says content generated and published before 2 August 2026 does not have to be marked retroactively.
- **The technique.** The Act still names no technology. The Code of Practice, which is voluntary, expects signed metadata and an imperceptible watermark; a non-signatory shows compliance by other adequate means.
- **The penalty ceiling.** Article 99(4)(g): up to EUR 15 million or 3% of worldwide annual turnover, whichever is higher, and for SMEs and start-ups whichever is lower.
- **The next date.** Code signatories must have an interoperable detection solution by 2 February 2027.

## Will fines start on 3 December?

We do not expect so, and it would be dishonest to sell the date that way. As of 12 October 2026 we could find no public enforcement action, fine or formal warning under Article 50 anywhere in the EU. Enforcement belongs to national market surveillance authorities, and at least one Member State had not designated its authority as of late August 2026 (law-firm note; secondary, UNVERIFIED). We check monthly and publish the result as the enforcement watch.

The reason to act before 2 December is plainer. The date is fixed in a regulation, the work takes weeks, and after it there is no transitional rule left to point to.

## What should be in place by then?

For each output your system generates: a machine-readable mark that survives ordinary handling, which in practice means a signed manifest and a watermark; a way to detect your own outputs; and a record of when marking started. If you call inference platforms, check what arrives: of the eleven we read, ten mark nothing and one marks in its own name ([the table](/platforms)).

## What this means if you ship AI generation

If you were live before 2 August, you have until 2 December and no longer. If you launched since, you are already inside the rule. If you route between platforms or self-host open models, the marking is yours to add after generation, whichever model served the request. The [Article 50 tracker](/law/eu-ai-act-article-50) carries the texts and the dates each was read.

SignUnder signs each output with Content Credentials, an invisible watermark and a fingerprint on record, and gives you a public page to detect your own outputs; it is in free early access ([get a key](/dashboard), [docs](/docs)). Until your company is listed it signs with SignUnder's test certificate, and under your own once it is. The free [checker](/checker) tells you which other rules apply to you.

*SignUnder is not a law firm and this is not legal advice. Every rule here cites its primary source and the date we read it.*
