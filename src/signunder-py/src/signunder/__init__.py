"""Python client for SignUnder: sign AI-generated images under your own name, with
Content Credentials, an invisible watermark and a fingerprint, and verify them.

Docs: https://signunder.com/docs
"""

from ._client import DEFAULT_BASE_URL, AsyncSignunder, Signunder
from ._models import Record, SignedAsset, SignunderError, Verification

__all__ = [
    "DEFAULT_BASE_URL",
    "AsyncSignunder",
    "Record",
    "SignedAsset",
    "Signunder",
    "SignunderError",
    "Verification",
]
__version__ = "0.1.0"
