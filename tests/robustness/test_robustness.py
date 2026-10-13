import pytest
from robustness import INFORMATIONAL, REQUIRED, Result, run


@pytest.fixture(scope="module")
def results() -> list[Result]:
    return run()


def test_every_image_meets_every_transform(results: list[Result]):
    images = {r.image for r in results}
    assert len(images) >= 7
    assert len(results) == len(images) * (len(REQUIRED) + len(INFORMATIONAL))


def test_watermark_survives_the_required_transforms_on_photographs(results: list[Result]):
    lost = [
        (r.image, r.transform)
        for r in results
        if r.photograph and r.transform in REQUIRED and not r.watermark_survived
    ]
    assert lost == []


@pytest.mark.parametrize("transform", ["resize 50%", "jpeg q50", "png to webp q80"])
def test_fingerprint_matches_after_recompression_and_resizing(results: list[Result], transform):
    missed = [
        (r.image, r.pdq_distance)
        for r in results
        if r.photograph and r.transform == transform and not r.fingerprint_matched
    ]
    assert missed == []


@pytest.mark.parametrize("transform", ["crop 10%", "screenshot (simulated)"])
def test_fingerprint_does_not_survive_cropping(results: list[Result], transform):
    """Not a goal: a record that PDQ is no fallback when an image is cropped or framed.
    If this starts failing the fingerprint got better and the documents should say so."""
    matched = [r.image for r in results if r.transform == transform and r.fingerprint_matched]
    assert len(matched) <= 1
