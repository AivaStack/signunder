---
title: "New York's synthetic performer law for advertisements"
description: "Since 9 June 2026 ads shown in New York must conspicuously disclose an AI-generated performer. $1,000 first violation, $5,000 after. Read 12 Oct 2026."
slug: new-york
type: law
date: 2026-10-13
updated: 2026-10-13
reviewed: 2026-10-13
authors: [eoin]
tags: [new-york, synthetic-performer, advertising, law, disclosure]
sources:
  - title: "New York General Business Law section 396-b, Advertisements, on the New York State Senate's site"
    url: https://www.nysenate.gov/legislation/laws/GBS/396-B
    read: 2026-10-12
  - title: "Senate Bill S8420-A (2025), signed 11 December 2025 as Chapter 617 of the Laws of 2025: bill text, sponsor memo and effective-date clause"
    url: https://www.nysenate.gov/legislation/bills/2025/S8420/amendment/A
    read: 2026-10-12
faq:
  - q: "What is a synthetic performer under New York law?"
    a: "A digitally created asset created, reproduced or modified by computer, using generative AI or a software algorithm, that is intended to create the impression of a human performer who is not recognisable as any identifiable natural performer (General Business Law section 396-b, read 12 October 2026)."
  - q: "When did the law take effect?"
    a: "The bill was signed on 11 December 2025 as Chapter 617 and takes effect on the 180th day after becoming law. By our count that is 9 June 2026."
  - q: "Who has to make the disclosure?"
    a: "The person who, for a commercial purpose, produces or creates the advertisement, where that person has actual knowledge that a synthetic performer is in it. The medium that publishes or carries the advertisement is outside the section."
  - q: "What is the penalty?"
    a: "A civil penalty of $1,000 for a first violation and $5,000 for any subsequent violation (section 396-b(3))."
  - q: "Does the law require a watermark or Content Credentials?"
    a: "No. It requires a conspicuous disclosure in the advertisement itself. It says nothing about metadata, watermarks or provenance."
related:
  - /law/california-sb-942-ab-853
  - /law/eu-ai-act-article-50
  - /labels/meta
draft: false
---

New York requires an advertisement to say so when it features a synthetic performer: an AI-generated or computer-made figure that looks like a human performer but is no identifiable real person. The duty sits in General Business Law section 396-b, added by Senate Bill S8420-A, signed on 11 December 2025 and in effect from 9 June 2026. It binds whoever produces or creates the advertisement, where they have actual knowledge. The penalty is $1,000 for a first violation and $5,000 for each one after. It is a visible-disclosure rule for advertisers; it does not ask for a machine-readable mark.

- In effect: **9 June 2026**, the 180th day after signature on 11 December 2025 (Chapter 617, Laws of 2025).
- Trigger: a **synthetic performer** in an advertisement, and **actual knowledge** on the part of the person producing it.
- Duty: **conspicuously disclose** in the advertisement that a synthetic performer is in it.
- Outside it: **audio advertisements**, AI used solely for **language translation** of a human performer, and promotion of expressive works that use the performer the same way.
- Penalty: **$1,000** first violation, **$5,000** each subsequent one.

Quotations are from the section as shown on the State Senate's site and from the bill text, both read on 12 October 2026.

## What is a synthetic performer?

Section 396-b(1) defines it as "a digitally created asset created, reproduced, or modified by computer", using generative artificial intelligence or a software algorithm, that is "intended to create the impression that the asset is engaging in an audiovisual and/or visual performance" of "a human performer who is not recognizable as any identifiable natural performer".

The last clause sets the boundary. A generated spokesperson who resembles nobody is a synthetic performer. A digital replica of a real, recognisable person is not; subdivision 5 leaves that to New York's existing privacy and publicity rights, saying the section does not limit, reduce or enlarge rights under Civil Rights Law sections 50, 50-f and 51. Generated scenery, products and backgrounds with no performer in them do not meet the definition either.

## Who does it bind?

Subdivision 3 applies to "any person engaged in the business of dealing in any property or service who for any commercial purpose" produces or creates an advertisement containing a synthetic performer. The duty applies "where such person has actual knowledge".

Subdivision 8 keeps the carrier out: "Nothing in this section shall apply to any medium used for advertising" by which an offending advertisement is published or disseminated. A broadcaster, publisher or ad platform that only runs the advertisement is not the target. Subdivision 6 adds that the protections of section 230 of the federal Communications Decency Act are neither limited nor enlarged.

The section does not define "advertisement", and it does not say which office enforces the penalty.

## What exactly is required?

The person "shall conspicuously disclose in such advertisement that a synthetic performer is in such advertisement". The statute does not define "conspicuously" and prescribes no wording, size, placement or duration.

## What is exempt?

- **Expressive works.** Subdivision 4: the section "shall not apply to advertisements and promotional materials for expressive works", including motion pictures and television programmes, provided the use of the synthetic performer in the promotion is "consistent with its use in the expressive work". A trailer for a film with a generated character needs no disclosure.
- **Audio.** Subdivision 7(a) exempts audio advertisements.
- **Translation.** Subdivision 7(b) exempts cases where the use of AI "solely involves the language translation of a human performer".

## What is the penalty?

Subdivision 3: "A violation of this subdivision shall result in a civil penalty of one thousand dollars for a first violation" and "five thousand dollars for any subsequent violation." We found no reported enforcement action as of 12 October 2026; that is a search result, not a confirmation that none exists.

## How does it differ from the provenance laws?

| | New York GBL 396-b | California AI Transparency Act | EU AI Act Article 50(2) |
|---|---|---|---|
| Who | The advertiser producing the ad | Generative AI providers over 1,000,000 monthly users | Providers of generative systems |
| What | A visible disclosure in the ad | A latent disclosure in the file, a manifest option, a detection tool | A machine-readable, detectable mark |
| Content | Visual and audiovisual ads with a synthetic performer | Image, video and audio | Audio, image, video and text |
| Knowledge | Actual knowledge required | Not a condition for providers | Not a condition |
| Penalty | $1,000, then $5,000 | $5,000 per violation per day | Up to EUR 15 million or 3% of turnover |

New York regulates the person who publishes a claim to the public; California and the EU regulate the system that made the file. The two meet at the question of knowledge. An advertiser buying creative from an agency or generating it in a third-party tool has to know whether a performer in it is synthetic, and provenance data in the file is the record that answers that. The statute does not require the record, only the disclosure.

## What this means if you ship AI generation

If you advertise in New York with generated people, the disclosure goes in the advertisement and has to be conspicuous; nothing in a file's metadata substitutes for it. If you build generation tools that advertisers use, the section does not bind you, but your customers' duty depends on knowing what your tool produced, and a signed manifest on the output is how that knowledge travels with the file through an agency and an approval chain.

SignUnder's manifests record that an asset was generated and by which service, which is the evidence an advertiser needs to decide whether to disclose; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-13 — First published, from General Business Law section 396-b and the text of S8420-A as read on 12 October 2026.

*SignUnder is not a law firm and this is not legal advice. Every rule here cites its primary source and the date we read it.*
