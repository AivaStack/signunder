import io

import httpx
import pytest

from signunder import AsyncSignunder, Signunder, SignunderError

PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 16
JPEG = b"\xff\xd8\xff\xe0" + b"\x00" * 16
WEBP = b"RIFF\x00\x00\x00\x00WEBPVP8 " + b"\x00" * 8


def api(handler):
    """A client whose requests go to ``handler`` instead of the network."""
    seen = []

    def record(request):
        seen.append(request)
        return handler(request)

    client = Signunder(
        "su_test_key", http_client=httpx.Client(transport=httpx.MockTransport(record))
    )
    return client, seen


def signed(request):
    return httpx.Response(
        200,
        content=b"signed bytes",
        headers={
            "content-type": "image/png",
            "SignUnder-Record-Id": "01REC",
            "SignUnder-Manifest-Hash": "ab12",
        },
    )


def test_sign_sends_the_key_and_the_file_and_returns_the_record_id(tmp_path):
    client, seen = api(signed)
    picture = tmp_path / "picture.png"
    picture.write_bytes(PNG)

    result = client.sign(picture)

    assert result.content == b"signed bytes"
    assert (result.record_id, result.manifest_hash, result.content_type) == (
        "01REC",
        "ab12",
        "image/png",
    )
    request = seen[0]
    assert request.url == "https://signunder-staging-api.fly.dev/v1/sign"
    assert request.headers["Authorization"] == "Bearer su_test_key"
    body = request.content
    assert b'filename="picture.png"' in body and b"Content-Type: image/png" in body and PNG in body
    assert result.save(tmp_path / "out.png").read_bytes() == b"signed bytes"


@pytest.mark.parametrize(
    "image, kind",
    [
        (JPEG, "image/jpeg"),
        (PNG, "image/png"),
        (WEBP, "image/webp"),
        (io.BytesIO(PNG), "image/png"),
    ],
)
def test_the_image_type_comes_from_its_bytes(image, kind):
    client, seen = api(signed)

    client.sign(image)

    assert f"Content-Type: {kind}".encode() in seen[0].content


def test_unknown_or_empty_images_are_refused_before_any_request():
    client, seen = api(signed)

    with pytest.raises(ValueError, match="JPEG, PNG and WebP"):
        client.sign(b"GIF89a....")
    with pytest.raises(ValueError, match="empty"):
        client.sign(b"")
    assert seen == []


def test_api_refusals_carry_the_detail_and_any_support_reference():
    def failing(request):
        return httpx.Response(
            502,
            json={
                "status": 502,
                "detail": "Signing failed. Quote reference 55c6da6b1e0bba08 to SignUnder support.",
            },
        )

    client, _ = api(failing)

    with pytest.raises(SignunderError) as error:
        client.sign(PNG)
    assert error.value.status_code == 502
    assert error.value.reference == "55c6da6b1e0bba08"
    assert "Signing failed" in error.value.detail


def test_verify_reads_the_summary_and_a_missing_manifest_is_not_an_error():
    def verify(request):
        return httpx.Response(
            200,
            json={
                "manifest_found": True,
                "validation_state": "valid",
                "signed_by": "Test Company",
                "trust_status": "untrusted",
                "issued_at": "2026-10-16T06:52:31.1234567+00:00",
                "actions": ["c2pa.created"],
                "altered": False,
                "failures": [],
                "cert_subject": {"O": "Test Company"},
                "report": {"manifests": []},
            },
        )

    client, _ = api(verify)
    result = client.verify(PNG)
    assert result.manifest_found and result.signed_by == "Test Company"
    assert result.issued_at.isoformat() == "2026-10-16T06:52:31.123456+00:00"

    nothing, _ = api(lambda request: httpx.Response(200, json={"manifest_found": False}))
    assert nothing.verify(PNG).manifest_found is False


def test_get_record_is_none_for_an_unknown_id():
    def records(request):
        if request.url.path.endswith("/01UNKNOWN"):
            return httpx.Response(404, json={"detail": "No record"})
        return httpx.Response(
            200,
            json={
                "id": "01REC",
                "created_at": "2026-10-16T06:52:31.5+00:00",
                "modality": "image",
                "mime": "image/png",
                "size": 10,
                "bytes_sha256": "aa",
                "manifest_hash": "bb",
                "manifest_label": "urn:c2pa:x",
                "validation_state": "valid",
                "actions": ["c2pa.created"],
                "layers": {"manifest": True, "watermark": True, "fingerprint": True},
            },
        )

    client, _ = api(records)

    assert client.get_record("01UNKNOWN") is None
    record = client.get_record("01REC")
    assert record.layers["watermark"] and record.created_at.year == 2026


def test_a_key_is_required(monkeypatch):
    monkeypatch.delenv("SIGNUNDER_API_KEY", raising=False)
    with pytest.raises(ValueError, match="SIGNUNDER_API_KEY"):
        Signunder()
    monkeypatch.setenv("SIGNUNDER_API_KEY", "su_test_from_env")
    assert Signunder()._headers["Authorization"] == "Bearer su_test_from_env"


def test_an_unreachable_api_is_a_signunder_error():
    def down(request):
        raise httpx.ConnectError("refused", request=request)

    client, _ = api(down)
    with pytest.raises(SignunderError, match="could not be reached") as error:
        client.sign(PNG)
    assert error.value.status_code is None


async def test_the_async_client_signs_too():
    client = AsyncSignunder(
        "su_test_key", http_client=httpx.AsyncClient(transport=httpx.MockTransport(signed))
    )
    async with client:
        result = await client.sign(JPEG)
    assert result.record_id == "01REC"
