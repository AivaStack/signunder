# content/

Blog posts and reference pages for signunder.com, as plain Markdown. This folder is the source; the publication is `https://signunder.com`. The private `signunder-web` repository pulls this folder at build time and renders it. Nothing here is rendered from GitHub, and every page sets its canonical to signunder.com.

The plan behind this folder is `docs/marketing/SEO-GEO-PLAN.md`; the work list is `docs/marketing/CHECKLIST.md`.

## Folders

| Folder | What goes there | URL |
|---|---|---|
| `blog/` | Dated posts, `YYYY-MM-DD-slug.md` | `/blog/{slug}` |
| `law/` | One hub per jurisdiction, plus `changes.md` (hand-written entries for the law feed) | `/law/{slug}`, `/law/changes` |
| `platforms/` | One page per inference platform, plus `_index.md` for the table | `/platforms/{slug}` |
| `models/` | One page per open model (P1) | `/models/{slug}` |
| `labels/` | One page per publishing platform's labelling policy | `/labels/{slug}` |
| `guides/` | Evergreen explainers | `/guides/{slug}` |
| `compare/` | One page per vendor comparison | `/compare/{slug}` |
| `_templates/` | Start every new file from one of these | — |

Slugs: lower-case, hyphens, no dates (dates live in frontmatter). A slug never changes without a redirect in `signunder-web`'s `vercel.json`.

## Frontmatter

```yaml
---
title: "Does Replicate sign your AI output? What its terms say"      # ≤70 chars, the H1
description: "Replicate attaches no C2PA manifest or watermark. Its terms make you responsible for output. Read 9 Oct 2026."   # ≤155 chars, the meta description
slug: replicate
type: platform              # post | law | platform | model | label | guide | compare
date: 2026-10-21            # first published
updated: 2026-10-21         # last substantive change; drives sitemap lastModified and dateModified
reviewed: 2026-10-21        # last time every source was re-read, shown as "Last reviewed"
authors: [eoin]             # keys resolved by the site to the /about bio
tags: [replicate, article-50, platforms]
sources:
  - title: "Replicate Terms of Service"
    url: https://replicate.com/terms
    read: 2026-10-21
  - title: "Commission guidelines on Article 50 (20 July 2026)"
    url: https://…
    read: 2026-10-09
faq:
  - q: "Does Replicate add Content Credentials to generated images?"
    a: "No. As read on 21 October 2026, Replicate attaches no C2PA manifest and no watermark, and its terms make the customer responsible for output."
related:
  - /law/eu-ai-act-article-50
  - /guides/sign-under-your-own-name
draft: false
# canonical: https://signunder.com/platforms/replicate   # only if it must differ from the default
---
```

`date`, `updated` and `reviewed` are ISO dates. `sources[].read` is the date the source was read, not published. Every number in the body must be traceable to an entry in `sources`.

## Publishing on a date

A page appears on signunder.com once `draft: false` **and** its `date` has arrived (UTC, at build time). So a finished post can be pushed with `draft: false` days ahead: the website rebuilds at 00:07 UTC every day and publishes it that morning. Any push that changes `content/` on `main` also rebuilds the website, so edits go live within minutes. To preview the site as it will be on a given day, build `signunder-web` with `CONTENT_BUILD_DAY=YYYY-MM-DD` (and `CONTENT_INCLUDE_DRAFTS=1` to see drafts too).

Before marking a dated post ready, re-check its quoted clauses and claims against the live sources; if any have moved, update the text, and `reviewed` only if every source was re-read.

## House style (what makes a page citable)

1. **Answer first.** The first two sentences answer the question in the title. No preamble.
2. **In brief.** Hubs open with three to five bullets a reader can take away without reading on.
3. **Question-form headings.** H2s are the questions people type or ask: "Who does Article 50(2) apply to?", not "Scope".
4. **Every number has a source, inline.** "median 17 days (C2PA CPL JSON, read 8 Oct 2026)". The sources footer repeats URL and date.
5. **Name things precisely.** Regulation (EU) 2024/1689 Article 50(2); C2PA Certificate Policy v0.2; Conforming Products List; Commission guidelines of 20 July 2026. Entities, not pronouns.
6. **Dates everywhere.** Platforms and laws change. "As read on 9 October 2026" is a feature.
7. **UNVERIFIED and *secondary*** exactly as in `docs/ARCHITECTURE.md`: a claim not read from a primary source says so.
8. **Tables for anything comparative.** Columns are facts, not adjectives.
9. **A real byline.** Every page has an author with a bio on `/about`.
10. **Last reviewed.** Visible on every hub, from `reviewed`.
11. **FAQ block.** Three to five pairs in `faq`, rendered with `FAQPage` markup. Each answer stands alone.
12. **Not legal advice.** Once per page that touches law: "SignUnder is not a law firm and this is not legal advice. Every rule here cites its primary source and the date we read it."
13. **No fear.** The enforcement watch says "none found" as long as that is true. We sell the fixed deadline and the weeks of work, not a knock at the door.
14. **No filler.** No "in today's fast-paced world", no "it is important to note". Short paragraphs, one idea each.
15. **Nothing gated.** No email walls, no truncated posts.
16. **Quote clauses briefly.** When quoting a platform's terms, quote the sentence, give the URL and date, and do not reproduce more than needed.

## Hub pages: standing sections

In this order. The site renders In brief, byline, Last reviewed, Changes, Sources and FAQ from frontmatter and headings; write the body between.

1. In brief (bullets)
2. The body, under question-form H2s
3. `## What this means if you…` (the practical paragraph for each persona it touches)
4. `## Changes` — dated bullets, newest first, one line each; also copy law changes into `law/changes.md`
5. FAQ (from frontmatter)
6. Sources (from frontmatter)

## Publishing

1. Copy the template; fill frontmatter; write.
2. Run the review checklist below.
3. Commit with a message naming the page (`content: add /platforms/replicate`).
4. Push. The `signunder-web` build pulls `content/`, renders, deploys; the deploy submits changed URLs to IndexNow.
5. Within a week: URL Inspection in Search Console shows it indexed; if not, request indexing.
6. Post the LinkedIn note the same day (`CHECKLIST.md` D5).
7. For law changes: entry in `law/changes.md` the same day, and bump the rule pack in `src/Signunder.Rules` if the rule itself changed.

## Review checklist (before every push)

- [ ] Title ≤70 characters and contains the target query's words
- [ ] Description ≤155 characters, states the answer, carries a date
- [ ] First two sentences answer the question
- [ ] Every number has an inline source and a `sources` entry with `read`
- [ ] Anything not from a primary source marked UNVERIFIED or *secondary*
- [ ] `updated` and `reviewed` set correctly
- [ ] At least two internal links to hubs; `related` filled
- [ ] FAQ has three to five pairs, each self-contained
- [ ] "Not legal advice" line present if the page touches law
- [ ] No template or licensed text copied in
- [ ] Reads aloud without filler

## Licence boundary

Words here are ours and open with the repository. Rendering code, components and anything derived from the shadcnblocks Zippay template stay in `signunder-web` and never enter this folder (ADR 0001).
