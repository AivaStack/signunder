"""Watermarking (TrustMark) and fingerprinting (PDQ) of still images."""

import io
import threading
from dataclasses import dataclass

import numpy as np
import pdqhash
from PIL import Image
from trustmark import TrustMark

PAYLOAD_BITS = 61
"""What TrustMark carries with BCH_5, its strongest standard error correction."""

ALGORITHM = "com.adobe.trustmark.Q"

_FORMATS = {"image/jpeg": "JPEG", "image/png": "PNG"}


class UnsupportedImage(ValueError):
    """The bytes are not an image of a type the worker handles."""


@dataclass(frozen=True)
class Fingerprint:
    pdq: bytes
    quality: int


class Marker:
    """Loads the TrustMark model once. Safe to share between threads."""

    def __init__(self) -> None:
        self._trustmark = TrustMark(
            verbose=False,
            model_type="Q",
            encoding_type=TrustMark.Encoding.BCH_5,
            device="cpu",
            loadRemover=False,
            loadBBoxDetector=True,
        )
        if self._trustmark.schemaCapacity() != PAYLOAD_BITS:
            raise RuntimeError("TrustMark's BCH_5 capacity is not what this worker assumes")
        # The model holds per-call state; inference is fast enough to serialise.
        self._lock = threading.Lock()

    def embed(self, asset: bytes, mime_type: str, payload: int) -> tuple[bytes, Fingerprint]:
        """Returns the watermarked image, in the same format, and its fingerprint."""
        if not 0 <= payload < 1 << PAYLOAD_BITS:
            raise ValueError(f"payload must be below 2^{PAYLOAD_BITS}")
        image = _open(asset, mime_type)

        alpha = image.getchannel("A") if "A" in image.getbands() else None
        with self._lock:
            marked = self._trustmark.encode(
                image.convert("RGB"), format(payload, f"0{PAYLOAD_BITS}b"), MODE="binary"
            )
        fingerprint = fingerprint_of(marked)
        if alpha is not None:
            marked.putalpha(alpha)

        out = io.BytesIO()
        extras = {k: image.info[k] for k in ("icc_profile", "exif") if image.info.get(k)}
        if mime_type == "image/jpeg":
            # High quality and no chroma subsampling, so re-encoding costs the mark little.
            marked.save(out, "JPEG", quality=95, subsampling=0, **extras)
        else:
            marked.save(out, "PNG", **extras)
        return out.getvalue(), fingerprint

    def decode(self, asset: bytes, mime_type: str) -> tuple[int | None, Fingerprint]:
        """Returns the payload, or None if no watermark was found, and the fingerprint."""
        image = _open(asset, mime_type).convert("RGB")
        return self.decode_image(image), fingerprint_of(image)

    def decode_image(self, image: Image.Image) -> int | None:
        """Looks for a payload in the whole image, then in the region TrustMark's detector
        picks out, which is what recovers a marked image shown inside a larger one (a
        screenshot). The second pass can report a payload in an unmarked image, so a
        payload means something only once it is found in the registry."""
        with self._lock:
            bits, present, _ = self._trustmark.decode(image, MODE="binary")
            if not present:
                bits, present, _ = self._trustmark.decode(image, MODE="binary", DETECTFIRST=True)
        return int(bits, 2) if present else None


def fingerprint_of(image: Image.Image) -> Fingerprint:
    bits, quality = pdqhash.compute(np.asarray(image.convert("RGB")))
    return Fingerprint(pdq=np.packbits(bits).tobytes(), quality=int(quality))


def hamming(a: bytes, b: bytes) -> int:
    """How many bits two PDQ hashes differ in."""
    return int.from_bytes(a, "big").__xor__(int.from_bytes(b, "big")).bit_count()


def _open(asset: bytes, mime_type: str) -> Image.Image:
    expected = _FORMATS.get(mime_type)
    if expected is None:
        raise UnsupportedImage(f"unsupported type {mime_type!r}")
    try:
        image = Image.open(io.BytesIO(asset))
        image.load()
    except Exception as e:  # Pillow raises many types for bad input
        raise UnsupportedImage(f"could not read the image: {e}") from e
    if image.format != expected:
        raise UnsupportedImage(f"the image is {image.format}, not {mime_type}")
    return image
