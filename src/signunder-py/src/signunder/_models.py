from __future__ import annotations

import re
from collections.abc import Mapping
from dataclasses import dataclass, field
from datetime import datetime
from os import PathLike
from pathlib import Path
from typing import Any


class SignunderError(Exception):
    """The SignUnder API refused a request, or could not be reached."""

    def __init__(self, status_code: int | None, detail: str):
        super().__init__(f"{status_code}: {detail}" if status_code else detail)
        #: The HTTP status the API answered with; None if it was not reached.
        self.status_code = status_code
        #: The API's explanation.
        self.detail = detail
        match = re.search(r"Quote reference (\w+)", detail)
        #: For a failure on SignUnder's side, the reference to quote to support.
        self.reference: str | None = match.group(1) if match else None


def _time(value: str | None) -> datetime | None:
    """An ISO 8601 time from the API. .NET writes one to seven fractional digits and
    Python before 3.11 reads exactly three or six, so the fraction is made six."""
    if not value:
        return None
    value = value.replace("Z", "+00:00")
    value = re.sub(r"\.(\d+)", lambda m: "." + (m.group(1) + "000000")[:6], value, count=1)
    return datetime.fromisoformat(value)


@dataclass(frozen=True)
class SignedAsset:
    """A signed image and the id of its registry record."""

    #: The signed file. Serve these bytes unchanged: re-encoding breaks the signature.
    content: bytes
    #: The file's media type, the same as the upload's.
    content_type: str
    #: The registry record for this output (also how its verify page links to it).
    record_id: str
    #: SHA-256 of the C2PA manifest store, lower-case hex.
    manifest_hash: str

    def save(self, path: str | PathLike) -> Path:
        """Writes the signed file to ``path`` and returns it."""
        target = Path(path)
        target.write_bytes(self.content)
        return target


@dataclass(frozen=True)
class Verification:
    """What an image's Content Credentials say, and whether they still hold."""

    #: False if the image carries no Content Credentials; the rest is then empty.
    manifest_found: bool
    #: ``invalid``, ``valid`` or ``trusted``.
    validation_state: str | None = None
    manifest_label: str | None = None
    #: The organisation, or failing that the common name, on the signing certificate.
    signed_by: str | None = None
    cert_subject: dict[str, str] = field(default_factory=dict)
    #: ``trusted`` if the certificate chains to the C2PA trust list, else ``untrusted``.
    trust_status: str | None = None
    #: The trusted timestamp on the signature, if it has one.
    issued_at: datetime | None = None
    #: The actions the manifest declares, for example ``c2pa.created``.
    actions: list[str] = field(default_factory=list)
    #: True if the image no longer matches what was signed.
    altered: bool = False
    #: C2PA validation failure codes.
    failures: list[str] = field(default_factory=list)
    #: The full crJSON validation report.
    report: dict[str, Any] | None = None

    @classmethod
    def from_json(cls, body: Mapping[str, Any]) -> Verification:
        if not body.get("manifest_found"):
            return cls(manifest_found=False)
        return cls(
            manifest_found=True,
            validation_state=body.get("validation_state"),
            manifest_label=body.get("manifest_label"),
            signed_by=body.get("signed_by"),
            cert_subject=dict(body.get("cert_subject") or {}),
            trust_status=body.get("trust_status"),
            issued_at=_time(body.get("issued_at")),
            actions=list(body.get("actions") or []),
            altered=bool(body.get("altered")),
            failures=list(body.get("failures") or []),
            report=body.get("report"),
        )


@dataclass(frozen=True)
class Record:
    """The registry's record of one signed output. It holds hashes, never the media."""

    id: str
    created_at: datetime
    #: For example ``image``.
    modality: str
    mime: str
    size: int
    #: SHA-256 of the signed file, lower-case hex.
    bytes_sha256: str
    #: SHA-256 of the C2PA manifest store, lower-case hex.
    manifest_hash: str
    manifest_label: str | None
    #: The state the output validated to when it was signed.
    validation_state: str
    actions: list[str]
    #: Which marks the output carries: ``manifest``, ``watermark``, ``fingerprint``.
    layers: dict[str, bool]
    #: The number in the output's invisible watermark, as 16 hex digits.
    wm_payload: str | None = None
    #: The output's PDQ perceptual hash, as 64 hex digits.
    fingerprint: str | None = None
    fingerprint_quality: int | None = None
    #: Where the output came from in proxy mode, for example ``fal``.
    provider: str | None = None
    model_name: str | None = None
    #: Where SignUnder serves its own copy (proxy mode), while it keeps one.
    media_url: str | None = None
    validation_report: dict[str, Any] | None = None

    @classmethod
    def from_json(cls, body: Mapping[str, Any]) -> Record:
        created = _time(body["created_at"])
        assert created is not None
        return cls(
            id=body["id"],
            created_at=created,
            modality=body["modality"],
            mime=body["mime"],
            size=int(body["size"]),
            bytes_sha256=body["bytes_sha256"],
            manifest_hash=body["manifest_hash"],
            manifest_label=body.get("manifest_label"),
            validation_state=body["validation_state"],
            actions=list(body.get("actions") or []),
            layers=dict(body.get("layers") or {}),
            wm_payload=body.get("wm_payload"),
            fingerprint=body.get("fingerprint"),
            fingerprint_quality=body.get("fingerprint_quality"),
            provider=body.get("provider"),
            model_name=body.get("model_name"),
            media_url=body.get("media_url"),
            validation_report=body.get("validation_report"),
        )
