import io
import secrets
from pathlib import Path

import pytest
from PIL import Image

from wm_worker.marking import PAYLOAD_BITS, Marker, UnsupportedImage, hamming

PHOTO = Path(__file__).parents[3] / "tests" / "robustness" / "images" / "earth.jpg"


@pytest.fixture(scope="session")
def marker() -> Marker:
    return Marker()


@pytest.fixture(scope="session")
def photo() -> bytes:
    return PHOTO.read_bytes()


def as_png(jpeg: bytes, mode: str = "RGB") -> bytes:
    out = io.BytesIO()
    Image.open(io.BytesIO(jpeg)).convert(mode).save(out, "PNG")
    return out.getvalue()


def test_jpeg_round_trip(marker: Marker, photo: bytes):
    payload = secrets.randbits(PAYLOAD_BITS)

    marked, fingerprint = marker.embed(photo, "image/jpeg", payload)

    image = Image.open(io.BytesIO(marked))
    assert image.format == "JPEG"
    assert image.size == Image.open(io.BytesIO(photo)).size
    found, decoded_fingerprint = marker.decode(marked, "image/jpeg")
    assert found == payload
    assert len(fingerprint.pdq) == 32
    assert fingerprint.quality > 50
    # The fingerprint is of the pixels before the final JPEG encode; the file's is close.
    assert hamming(fingerprint.pdq, decoded_fingerprint.pdq) <= 8


def test_png_round_trip_keeps_transparency(marker: Marker, photo: bytes):
    source = Image.open(io.BytesIO(as_png(photo, "RGBA")))
    source.putalpha(128)
    out = io.BytesIO()
    source.save(out, "PNG")
    payload = (1 << PAYLOAD_BITS) - 1

    marked, _ = marker.embed(out.getvalue(), "image/png", payload)

    image = Image.open(io.BytesIO(marked))
    assert image.format == "PNG"
    assert image.mode == "RGBA"
    assert image.getchannel("A").getextrema() == (128, 128)
    assert marker.decode(marked, "image/png")[0] == payload


def test_webp_round_trip(marker: Marker, photo: bytes):
    out = io.BytesIO()
    Image.open(io.BytesIO(photo)).save(out, "WEBP", quality=90)
    payload = 987654321

    marked, _ = marker.embed(out.getvalue(), "image/webp", payload)

    assert Image.open(io.BytesIO(marked)).format == "WEBP"
    assert marker.decode(marked, "image/webp")[0] == payload


def test_the_mark_is_hard_to_see(marker: Marker, photo: bytes):
    import numpy as np

    marked, _ = marker.embed(as_png(photo), "image/png", 12345)

    before = np.asarray(Image.open(io.BytesIO(photo)).convert("RGB"), dtype=np.float64)
    after = np.asarray(Image.open(io.BytesIO(marked)).convert("RGB"), dtype=np.float64)
    psnr = 10 * np.log10(255**2 / np.mean((before - after) ** 2))
    assert psnr > 38


def test_unmarked_image_has_no_payload(marker: Marker, photo: bytes):
    found, fingerprint = marker.decode(photo, "image/jpeg")

    assert found is None
    assert len(fingerprint.pdq) == 32


def test_a_payload_of_another_width_is_not_a_watermark(marker: Marker):
    # TrustMark's detector pass reads 68 bits from this unmarked photograph; our marks
    # carry 61, and the API cannot take a wider number.
    aurora = (PHOTO.parent / "aurora.jpg").read_bytes()

    found, _ = marker.decode(aurora, "image/jpeg")

    assert found is None


def test_marking_barely_moves_the_fingerprint(marker: Marker, photo: bytes):
    _, original = marker.decode(photo, "image/jpeg")
    _, marked = marker.embed(photo, "image/jpeg", 1)

    assert hamming(original.pdq, marked.pdq) <= 8


@pytest.mark.parametrize("payload", [-1, 1 << PAYLOAD_BITS])
def test_payload_out_of_range_is_refused(marker: Marker, photo: bytes, payload: int):
    with pytest.raises(ValueError):
        marker.embed(photo, "image/jpeg", payload)


@pytest.mark.parametrize(
    ("asset", "mime_type"),
    [
        (b"not an image", "image/jpeg"),
        (b"", "image/png"),
        (b"x", "image/gif"),
        (b"x", "image/webp"),
    ],
)
def test_bad_input_is_refused(marker: Marker, asset: bytes, mime_type: str):
    with pytest.raises(UnsupportedImage):
        marker.decode(asset, mime_type)


def test_type_must_match_the_bytes(marker: Marker, photo: bytes):
    with pytest.raises(UnsupportedImage):
        marker.embed(photo, "image/png", 1)
