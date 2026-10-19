"""Against the real service, only when asked: SIGNUNDER_LIVE=1 and SIGNUNDER_API_KEY set."""

import os
from pathlib import Path

import pytest

from signunder import Signunder

IMAGE = Path(__file__).parents[3] / "tests" / "robustness" / "images" / "mars.jpg"

pytestmark = pytest.mark.skipif(
    os.environ.get("SIGNUNDER_LIVE") != "1" or not os.environ.get("SIGNUNDER_API_KEY"),
    reason="set SIGNUNDER_LIVE=1 and SIGNUNDER_API_KEY to sign against the live service",
)


def test_signs_verifies_and_reads_back_the_record():
    with Signunder() as client:
        signed = client.sign(IMAGE)
        verification = client.verify(signed.content)
        record = client.get_record(signed.record_id)

    assert verification.manifest_found and not verification.altered
    assert "c2pa.created" in verification.actions
    assert record is not None and record.layers == {
        "manifest": True,
        "watermark": True,
        "fingerprint": True,
    }
