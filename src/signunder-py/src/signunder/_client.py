from __future__ import annotations

import os
from os import PathLike
from pathlib import Path
from typing import IO, Any
from urllib.parse import quote

import httpx
from typing_extensions import Self

from ._models import Record, SignedAsset, SignunderError, Verification

#: Early access runs on SignUnder's staging service; production will have its own address.
DEFAULT_BASE_URL = "https://signunder-staging-api.fly.dev"

#: A path, the image's bytes, or an open binary file.
Image = str | PathLike | bytes | bytearray | IO[bytes]

_EXTENSIONS = {
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".png": "image/png",
    ".webp": "image/webp",
}


def _sniff(head: bytes) -> str | None:
    if head.startswith(b"\xff\xd8\xff"):
        return "image/jpeg"
    if head.startswith(b"\x89PNG\r\n\x1a\n"):
        return "image/png"
    if head[:4] == b"RIFF" and head[8:12] == b"WEBP":
        return "image/webp"
    return None


def _upload(image: Image, content_type: str | None, filename: str | None) -> tuple[str, bytes, str]:
    """The multipart file tuple for ``image``: its name, bytes and media type."""
    if isinstance(image, (str, PathLike)):
        path = Path(image)
        data = path.read_bytes()
        filename = filename or path.name
    elif isinstance(image, (bytes, bytearray)):
        data = bytes(image)
    else:
        data = image.read()
        filename = filename or Path(getattr(image, "name", "") or "").name or None
    if not data:
        raise ValueError("The image is empty.")
    kind = content_type or _sniff(data[:16]) or _EXTENSIONS.get(Path(filename or "").suffix.lower())
    if kind not in _EXTENSIONS.values():
        raise ValueError(
            "SignUnder signs JPEG, PNG and WebP images; pass content_type if the file is one."
        )
    return (
        filename
        or "image" + {"image/jpeg": ".jpg", "image/png": ".png", "image/webp": ".webp"}[kind],
        data,
        kind,
    )


def _raise_unless_ok(response: httpx.Response) -> None:
    if response.is_success:
        return
    detail = response.reason_phrase or "Request failed"
    try:
        body = response.json()
        detail = body.get("detail") or body.get("title") or detail
    except ValueError:
        pass
    raise SignunderError(response.status_code, detail)


def _signed(response: httpx.Response, content_type: str) -> SignedAsset:
    _raise_unless_ok(response)
    record_id = response.headers.get("SignUnder-Record-Id")
    manifest_hash = response.headers.get("SignUnder-Manifest-Hash")
    if not record_id or not manifest_hash:
        raise SignunderError(
            response.status_code, "The response had no record id or manifest hash."
        )
    media_type = response.headers.get("content-type", content_type).split(";")[0].strip()
    return SignedAsset(response.content, media_type, record_id, manifest_hash)


def _settings(api_key: str | None, base_url: str | None) -> tuple[str, str]:
    key = api_key or os.environ.get("SIGNUNDER_API_KEY")
    if not key:
        raise ValueError(
            "No API key: pass api_key or set SIGNUNDER_API_KEY. Get one at https://signunder.com/dashboard."
        )
    return key, (base_url or os.environ.get("SIGNUNDER_BASE_URL") or DEFAULT_BASE_URL).rstrip("/")


class Signunder:
    """Calls the SignUnder API. Reuse one client; close it, or use it as a context manager.

    >>> from signunder import Signunder
    >>> with Signunder() as client:                     # key from SIGNUNDER_API_KEY
    ...     signed = client.sign("picture.png")
    ...     signed.save("picture.signed.png")
    """

    def __init__(
        self,
        api_key: str | None = None,
        *,
        base_url: str | None = None,
        timeout: float = 90.0,
        http_client: httpx.Client | None = None,
    ):
        key, url = _settings(api_key, base_url)
        self._owns_http = http_client is None
        self._http = http_client or httpx.Client(timeout=timeout)
        self._base = url
        self._headers = {"Authorization": f"Bearer {key}", "User-Agent": "signunder-python/0.1.0"}

    def sign(
        self, image: Image, *, content_type: str | None = None, filename: str | None = None
    ) -> SignedAsset:
        """Signs an image with your company's key: Content Credentials, an invisible
        watermark and a fingerprint on record. Returns the signed file and its record id."""
        upload = _upload(image, content_type, filename)
        response = self._send("POST", "/v1/sign", files={"file": upload})
        return _signed(response, upload[2])

    def verify(
        self, image: Image, *, content_type: str | None = None, filename: str | None = None
    ) -> Verification:
        """Reads and validates the Content Credentials in any image. An image without
        any is not an error: ``manifest_found`` is False."""
        response = self._send(
            "POST", "/v1/verify", files={"file": _upload(image, content_type, filename)}
        )
        _raise_unless_ok(response)
        return Verification.from_json(response.json())

    def get_record(self, record_id: str) -> Record | None:
        """One of your company's records, or None if you have none with that id."""
        response = self._send("GET", f"/v1/records/{quote(record_id, safe='')}")
        if response.status_code == 404:
            return None
        _raise_unless_ok(response)
        return Record.from_json(response.json())

    def _send(self, method: str, path: str, **kwargs: Any) -> httpx.Response:
        try:
            return self._http.request(method, self._base + path, headers=self._headers, **kwargs)
        except httpx.HTTPError as error:
            raise SignunderError(None, f"SignUnder could not be reached: {error}") from error

    def close(self) -> None:
        if self._owns_http:
            self._http.close()

    def __enter__(self) -> Self:
        return self

    def __exit__(self, *exc: object) -> None:
        self.close()


class AsyncSignunder:
    """The same calls as :class:`Signunder`, for asyncio.

    >>> async with AsyncSignunder() as client:
    ...     signed = await client.sign(image_bytes)
    """

    def __init__(
        self,
        api_key: str | None = None,
        *,
        base_url: str | None = None,
        timeout: float = 90.0,
        http_client: httpx.AsyncClient | None = None,
    ):
        key, url = _settings(api_key, base_url)
        self._owns_http = http_client is None
        self._http = http_client or httpx.AsyncClient(timeout=timeout)
        self._base = url
        self._headers = {"Authorization": f"Bearer {key}", "User-Agent": "signunder-python/0.1.0"}

    async def sign(
        self, image: Image, *, content_type: str | None = None, filename: str | None = None
    ) -> SignedAsset:
        upload = _upload(image, content_type, filename)
        response = await self._send("POST", "/v1/sign", files={"file": upload})
        return _signed(response, upload[2])

    async def verify(
        self, image: Image, *, content_type: str | None = None, filename: str | None = None
    ) -> Verification:
        response = await self._send(
            "POST", "/v1/verify", files={"file": _upload(image, content_type, filename)}
        )
        _raise_unless_ok(response)
        return Verification.from_json(response.json())

    async def get_record(self, record_id: str) -> Record | None:
        response = await self._send("GET", f"/v1/records/{quote(record_id, safe='')}")
        if response.status_code == 404:
            return None
        _raise_unless_ok(response)
        return Record.from_json(response.json())

    async def _send(self, method: str, path: str, **kwargs: Any) -> httpx.Response:
        try:
            return await self._http.request(
                method, self._base + path, headers=self._headers, **kwargs
            )
        except httpx.HTTPError as error:
            raise SignunderError(None, f"SignUnder could not be reached: {error}") from error

    async def close(self) -> None:
        if self._owns_http:
            await self._http.aclose()

    async def __aenter__(self) -> Self:
        return self

    async def __aexit__(self, *exc: object) -> None:
        await self.close()
