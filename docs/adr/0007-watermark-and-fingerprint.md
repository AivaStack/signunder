# ADR 0007 — Watermark and fingerprint

- **Status:** Accepted
- **Date:** 13 October 2026
- **Deciders:** Eoin

## Context

docs/ARCHITECTURE.md §1.3 and §1.7 call for an invisible watermark and a perceptual fingerprint on every output, with the watermark payload recorded in the manifest as a soft binding so that manifest and watermark cross-reference. A manifest is stripped by most platforms; the other two layers are what remain. §4.3 names TrustMark and PDQ and assumes a payload of about 100 bits, "enough for a registry ID".

## Decision

- **Watermark: TrustMark, model Q, BCH_5 error correction**, pinned at `trustmark==0.9.2`. BCH_5 leaves **61 bits** of payload. A record id is a 128-bit ULID and does not fit, so each record gets its own random 61-bit payload, stored in `asset_record.wm_payload` under a unique index.
- **Fingerprint: PDQ** (256 bits) of the watermarked image, stored with PDQ's own 0 to 100 quality score.
- **Order: watermark, then sign.** The watermark changes pixels, and the manifest's hard binding covers them, so `/v1/sign` calls the worker first and signs what it returns.
- **Soft binding.** The manifest carries a `c2pa.soft-binding` assertion with `alg = com.adobe.trustmark.Q` and the payload as eight big-endian bytes. The sidecar takes that assertion out of the JSON manifest definition and adds it as a typed assertion, because a value left in the JSON is stored as CBOR text, not bytes. In the definition the value is base64.
- **Output encoding.** A JPEG is re-encoded at quality 95 without chroma subsampling; a PNG stays lossless and keeps its alpha channel. ICC profile and EXIF are carried over.
- **Decoding** tries the whole image, then the region TrustMark's detector picks out. The second pass is what recovers a marked image shown inside a larger one.
  - *Added 15 Oct 2026:* the detector pass works on a copy shrunk to at most 512 pixels on the longer side. At full size it needed about 2 GB on a 1280×853 photograph and the staging worker was killed for memory. The robustness table is identical before and after.
  - *Added 15 Oct 2026:* a payload whose width is not 61 bits is not a watermark. The detector pass read 68 bits from an unmarked photograph.
- **Transport.** The worker is a Python gRPC service (`proto/signunder/wm/v1/wm.proto`) called inside the request, like the sidecar. It is optional: with no `Watermark:Address` the API signs unmarked and the record says so.
- **Model weights are baked into the worker image**, so it starts with no network.

## Consequences

Measured by `make robustness` on four photographs (13 October 2026):

- The watermark survived resize to 50%, JPEG q50, a 10% crop, a simulated screenshot and PNG to WebP on all four, and also JPEG q30 and resize to 25%. A 25% crop lost it on two of four.
- Without the detector pass the watermark survived the simulated screenshot on one of four.
- The fingerprint stayed within the usual match distance (31, *from memory*, UNVERIFIED) after resizing and recompression, and on none of four after either crop or the screenshot. **PDQ is not a fallback for cropped or framed images.**
- On three synthetic images the watermark survived 23 of 24 cases; a flat colour lost it at JPEG q50.

Other consequences:

- The detector pass can report a payload in an unmarked image (seen on a synthetic gradient). A payload is evidence only when it matches a registry record.
- Marked JPEGs are larger: a 350 KB photograph came back at 784 KB.
- The worker image is about 1.6 GB (PyTorch and roughly 150 MB of weights) and the worker serialises inference.
- The sample is four photographs, none of them model output, and the screenshot is simulated. The numbers are a first reading, not a survival rate to quote.

## Open

- Check `com.adobe.trustmark.Q` against the C2PA soft-binding algorithm list, and TrustMark's licence against §4.3's "Apache 2.0". Neither was read from a primary source.
- A larger image set including real model output, and real screenshots and platform round trips (§5, Platform survival).
- JPEG output quality against file size.
- A fingerprint or matching method that survives cropping, if `detect` needs one.
- Moving marking and signing behind the job queue (§4.2).
