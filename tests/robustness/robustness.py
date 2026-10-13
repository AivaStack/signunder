"""Watermark and fingerprint survival suite (SIGNUNDER.md §5).

Marks each image, puts it through the changes a shared image commonly suffers, and
records whether the watermark payload still decodes and how far the PDQ fingerprint
moved. Run it as a table with `make robustness`; `test_robustness.py` asserts on it.
"""

import hashlib
import io
from collections.abc import Callable
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from wm_worker.marking import PAYLOAD_BITS, Marker, fingerprint_of, hamming

IMAGES = Path(__file__).parent / "images"

PDQ_MATCH_DISTANCE = 31
"""Hamming distance at or under which two PDQ hashes are treated as the same image.
The figure commonly quoted for PDQ (*from memory*, UNVERIFIED against Meta's paper)."""


def _reencode(image: Image.Image, fmt: str, **options) -> Image.Image:
    out = io.BytesIO()
    image.save(out, fmt, **options)
    return Image.open(io.BytesIO(out.getvalue())).convert("RGB")


def _resize(image: Image.Image, factor: float) -> Image.Image:
    w, h = image.size
    return image.resize((max(1, round(w * factor)), max(1, round(h * factor))), Image.LANCZOS)


def _crop(image: Image.Image, fraction: float) -> Image.Image:
    """Removes `fraction` of the width and of the height, half from each edge."""
    w, h = image.size
    dx, dy = round(w * fraction / 2), round(h * fraction / 2)
    return image.crop((dx, dy, w - dx, h - dy))


def _screenshot(image: Image.Image) -> Image.Image:
    """A stand-in for a screenshot: shown at 83% on a white page, captured as PNG."""
    shown = _resize(image, 0.83)
    page = Image.new("RGB", (shown.width + 60, shown.height + 80), "white")
    page.paste(shown, (30, 40))
    return _reencode(page, "PNG")


# The five the plan requires, then two harsher ones reported for information.
REQUIRED: dict[str, Callable[[Image.Image], Image.Image]] = {
    "resize 50%": lambda im: _resize(im, 0.5),
    "jpeg q50": lambda im: _reencode(im, "JPEG", quality=50),
    "crop 10%": lambda im: _crop(im, 0.10),
    "screenshot (simulated)": _screenshot,
    "png to webp q80": lambda im: _reencode(_reencode(im, "PNG"), "WEBP", quality=80),
}
INFORMATIONAL: dict[str, Callable[[Image.Image], Image.Image]] = {
    "jpeg q30": lambda im: _reencode(im, "JPEG", quality=30),
    "resize 25%": lambda im: _resize(im, 0.25),
    "crop 25%": lambda im: _crop(im, 0.25),
}
TRANSFORMS = REQUIRED | INFORMATIONAL


def _synthetic() -> dict[str, Image.Image]:
    """Hard cases for an invisible mark: little texture to hide it in."""
    gradient = Image.new("RGB", (1024, 768))
    gradient.putdata(
        [(x * 255 // 1023, y * 255 // 767, 128) for y in range(768) for x in range(1024)]
    )
    graphic = Image.new("RGB", (1024, 768), "white")
    draw = ImageDraw.Draw(graphic)
    for i in range(12):
        draw.text(
            (60, 50 + i * 55), f"Line {i + 1}: generated diagram text, no photograph", "black"
        )
    draw.rectangle((640, 120, 940, 420), outline="navy", width=6)
    draw.ellipse((680, 460, 900, 680), fill="firebrick")
    flat = Image.new("RGB", (1024, 768), (40, 90, 160))
    return {"synthetic gradient": gradient, "synthetic graphic": graphic, "synthetic flat": flat}


def load_images() -> dict[str, tuple[Image.Image, bool]]:
    """Name to (image, is a photograph)."""
    photos = {p.stem: (Image.open(p).convert("RGB"), True) for p in sorted(IMAGES.glob("*.jpg"))}
    return photos | {name: (image, False) for name, image in _synthetic().items()}


@dataclass(frozen=True)
class Result:
    image: str
    photograph: bool
    transform: str
    watermark_survived: bool
    pdq_distance: int

    @property
    def fingerprint_matched(self) -> bool:
        return self.pdq_distance <= PDQ_MATCH_DISTANCE


def run(marker: Marker | None = None) -> list[Result]:
    marker = marker or Marker()
    results = []
    for name, (image, photograph) in load_images().items():
        # Fixed per image, so a run is repeatable.
        digest = hashlib.sha256(name.encode()).digest()
        payload = int.from_bytes(digest[:8], "big") >> (64 - PAYLOAD_BITS)
        out = io.BytesIO()
        image.save(out, "PNG")
        marked_bytes, fingerprint = marker.embed(out.getvalue(), "image/png", payload)
        marked = Image.open(io.BytesIO(marked_bytes)).convert("RGB")
        for transform, apply in TRANSFORMS.items():
            changed = apply(marked)
            results.append(
                Result(
                    image=name,
                    photograph=photograph,
                    transform=transform,
                    watermark_survived=marker.decode_image(changed) == payload,
                    pdq_distance=hamming(fingerprint.pdq, fingerprint_of(changed).pdq),
                )
            )
    return results


def table(results: list[Result]) -> str:
    images = list(dict.fromkeys(r.image for r in results))
    by = {(r.image, r.transform): r for r in results}
    width = max(len(i) for i in images)
    lines = [
        "Each cell: watermark decoded (yes/NO), then PDQ distance from the marked original",
        f"(a fingerprint match is {PDQ_MATCH_DISTANCE} or less).",
        "",
        " " * 24 + "".join(f"{i:>{width + 2}}" for i in images),
    ]
    for transform in TRANSFORMS:
        cells = []
        for image in images:
            r = by[image, transform]
            cells.append(
                f"{('yes' if r.watermark_survived else 'NO') + f' {r.pdq_distance:>3}':>{width + 2}}"
            )
        lines.append(f"{transform:<24}" + "".join(cells))
    lines.append("")
    for label, wanted in (("photographs", True), ("synthetic images", False)):
        subset = [r for r in results if r.photograph is wanted]
        wm = sum(r.watermark_survived for r in subset)
        fp = sum(r.fingerprint_matched for r in subset)
        either = sum(r.watermark_survived or r.fingerprint_matched for r in subset)
        lines.append(
            f"{label}: watermark {wm}/{len(subset)}, fingerprint {fp}/{len(subset)}, "
            f"either {either}/{len(subset)}"
        )
    return "\n".join(lines)


if __name__ == "__main__":
    print(table(run()))
