---
title: "California's AI Transparency Act: SB 942 as amended by AB 853"
description: "Operative 2 August 2026 for providers over a million monthly users: latent disclosures, a free detection tool, $5,000. Read 12 Oct 2026."
slug: california-sb-942-ab-853
type: law
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [california, sb-942, ab-853, law, provenance]
sources:
  - title: "California Business and Professions Code, Division 8, Chapter 25 (sections 22757.1 to 22757.6), FindLaw's copy, current as of 1 January 2026; the Legislature's site is authoritative but blocks automated reads"
    url: https://codes.findlaw.com/ca/business-and-professions-code/bpc-sect-22757-3/
    read: 2026-10-12
  - title: "AB 853 (2025), Chapter 674, Statutes of 2025, chaptered 13 October 2025: bill page and digest at CalMatters Digital Democracy"
    url: https://calmatters.digitaldemocracy.org/bills/ca_202520260ab853
    read: 2026-10-12
  - title: "AB 853 on the California Legislature's site (bill text)"
    url: https://leginfo.legislature.ca.gov/faces/billNavClient.xhtml?bill_id=202520260AB853
    read: 2026-10-08
faq:
  - q: "Who is a covered provider under the California AI Transparency Act?"
    a: "A person that creates, codes or otherwise produces a generative AI system that has over 1,000,000 monthly visitors or users and is publicly accessible within California (section 22757.1(d), as read on 12 October 2026). Smaller providers are outside the Act's direct duties."
  - q: "When does it apply?"
    a: "The chapter is operative from 2 August 2026 (section 22757.6, as amended by AB 853). The duties on large online platforms and on GenAI system hosting platforms start on 1 January 2027; capture device manufacturers' duties start on 1 January 2028 for devices first produced for sale in California from that date."
  - q: "What must a latent disclosure contain?"
    a: "Where technically feasible and reasonable, the provider's name, the name and version of the system, the time and date of creation or alteration, and a unique identifier, directly or through a link to a permanent website; detectable by the provider's own detection tool, consistent with widely accepted industry standards, and permanent or extraordinarily difficult to remove (section 22757.3(b))."
  - q: "Is there a penalty for stripping provenance data?"
    a: "For large online platforms, yes, from 1 January 2027: section 22757.3.1(b) says a platform shall not, to the extent technically feasible, knowingly strip system provenance data or a digital signature that is compliant with widely adopted specifications. For providers, the duty is that their own disclosure be extraordinarily difficult to remove, and that licensees keep the capability."
  - q: "What is the fine?"
    a: "$5,000 per violation, collected in a civil action by the Attorney General, a city attorney or a county counsel, and each day of violation by a covered provider, large online platform or capture device manufacturer is a separate violation (section 22757.4)."
related:
  - /law/eu-ai-act-article-50
  - /platforms
  - /guides/sign-under-your-own-name
draft: false
---

California's AI Transparency Act requires large generative AI providers to embed a latent, machine-readable disclosure in every AI-generated image, video or audio file, to offer users a manifest disclosure, and to run a free public detection tool, from 2 August 2026. AB 853, signed on 13 October 2025, moved that date from 1 January 2026, and added duties for large online platforms and GenAI hosting platforms from 1 January 2027 and for capture device manufacturers from 1 January 2028. The penalty is $5,000 per violation, per day. The Act is narrower than the EU's Article 50 in who it binds and more specific in what the mark must contain.

- Operative: **2 August 2026** (section 22757.6).
- Covered provider: a GenAI system with **over 1,000,000 monthly visitors or users**, publicly accessible in California.
- Duties: **latent disclosure** in every image, video and audio output; a **manifest disclosure** option; a **free AI detection tool** with an API; licence terms that keep the disclosure.
- From **1 January 2027**: large online platforms must detect compliant provenance data, show it, and not knowingly strip it; hosting platforms must not knowingly host a system that omits disclosures.
- Penalty: **$5,000 per violation**, each day separate.

## Who does it bind?

Section 22757.1(d) defines a covered provider as "a person that creates, codes, or otherwise produces a generative artificial intelligence system" that "has over 1,000,000 monthly visitors or users" and "is publicly accessible within the geographic boundaries of the state" (read 12 October 2026, FindLaw's copy current as of 1 January 2026). The threshold is the system's audience, so a company offering generation under its own brand to that many users is the covered provider whatever model sits behind it. Below the threshold the Act's direct duties do not apply, though the platform and hosting duties from 2027 reach content and systems regardless of their provider's size.

Section 22757.5 exempts products that provide exclusively "non-user-generated video game, television, streaming, movie, or interactive experiences".

## What must a covered provider do?

Three things, from section 22757.3 and 22757.2:

1. **A manifest disclosure, on request.** The provider must offer users the option to include a disclosure that identifies content as AI-generated, which must be "clear, conspicuous, appropriate for the medium of the content" and "permanent or extraordinarily difficult to remove, to the extent it is technically feasible" (22757.3(a)).
2. **A latent disclosure, always.** In AI-generated image, video or audio content, the provider must include a latent disclosure that, where technically feasible and reasonable, conveys the provider's name, the name and version number of the system, the time and date of creation or alteration, and a unique identifier, directly or through a link to a permanent website. It must be detectable by the provider's own detection tool, "consistent with widely accepted industry standards", and "permanent or extraordinarily difficult to remove, to the extent it is technically feasible" (22757.3(b)). A signed C2PA manifest carrying those fields, paired with an invisible watermark for when the metadata is stripped, is the obvious shape.
3. **A free detection tool.** Available "at no cost to the user", publicly accessible, accepting uploads or a URL, outputting detected system provenance data but not personal provenance data, with "an application programming interface that allows a user to invoke the tool without visiting" the provider's site, and collecting no personal information beyond opt-in feedback contact details (22757.2).

Licensing has its own rules: a provider that licenses its system must require by contract that the licensee keep the latent-disclosure capability; if it learns the licensee has disabled it, it must revoke the licence within 96 hours; and "A third-party licensee shall cease using a licensed GenAI system after the license for the system has been revoked" (22757.3(c)).

## What changed with AB 853?

AB 853, Chapter 674 of the Statutes of 2025, chaptered on 13 October 2025 (CalMatters bill page, read 12 October 2026), did four things:

- **Moved the operative date** from 1 January 2026 to 2 August 2026, the same day the EU's Article 50 began to apply. Section 22757.6 now reads: "This chapter shall become operative on August 2, 2026."
- **Added large online platforms** (section 22757.3.1): a public-facing social media, file-sharing, mass messaging or search platform with more than 2,000,000 unique monthly users must, from 1 January 2027, "Detect whether any provenance data that is compliant with widely adopted specifications" is embedded in or attached to content it distributes, "Provide a user interface to disclose the availability of system provenance data" indicating AI generation or capture, and "shall not, to the extent technically feasible, knowingly strip any system provenance data" or compliant digital signature.
- **Added GenAI system hosting platforms:** from 1 January 2027, a hosting platform is prohibited "from knowingly making available a GenAI system that does not place disclosures" as the Act requires (bill digest, read 12 October 2026). This is the provision that reaches the inference platforms on our [platform pages](/platforms): a hosted model that emits no latent disclosure becomes the host's problem too.
- **Added capture devices:** from 1 January 2028, manufacturers of cameras, phones and recorders first produced for sale in California must offer users the option to include a latent disclosure in captured content, including the manufacturer's name.

## What is the penalty?

Section 22757.4: "five thousand dollars ($5,000) per violation to be collected in a civil action filed by the Attorney General, a city attorney, or a county counsel", and each day a covered provider, large online platform or capture device manufacturer is in violation is a separate violation. The same officials may seek injunctive relief and fees against a licensee that keeps using a revoked system.

## How does it compare with Article 50?

| | California AI Transparency Act | EU AI Act Article 50(2) |
|---|---|---|
| Who | Providers with over 1,000,000 monthly users, publicly accessible in California | Every provider of a generative system placed on the EU market, no size threshold |
| Since | 2 August 2026 | 2 August 2026; 2 December 2026 for systems on the market before then |
| What the mark must contain | Provider name, system and version, time and date, unique identifier | Not specified; machine-readable, detectable, effective, interoperable, robust, reliable |
| Detection | A free public tool with an API, required | A detection solution under the Code of Practice; interoperable by 2 February 2027 |
| Stripping | Platforms may not knowingly strip compliant data, from 2027 | The Code asks signatories to preserve marks |
| Penalty | $5,000 per violation per day | Up to EUR 15 million or 3% of turnover |

The two line up on the technique. A signed manifest with those four fields, a watermark that survives stripping, and a public detection endpoint satisfy the Californian text and the Code's measures at once; the [EU tracker](/law/eu-ai-act-article-50) covers the other side.

## What this means if you ship AI generation

If you have over a million monthly users and any of them are in California, the latent disclosure, the manifest option and the free detection tool have been due since 2 August 2026. If you are smaller, the direct duties do not reach you, but from 2027 the platforms your users post to will be reading compliant provenance data and showing it, and the hosts your models run on will be answerable for systems that emit none.

SignUnder's manifests carry the four Californian fields, its watermark is the fallback for stripping, and its verify endpoint is the public detection tool with an API; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from sections 22757.1 to 22757.6 as current on 1 January 2026 and the AB 853 digest.

*SignUnder is not a law firm and this is not legal advice. The Legislature's text is authoritative; the copy read is noted with its currency date.*
