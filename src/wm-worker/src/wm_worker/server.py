"""gRPC server for the watermark worker."""

import logging
import os
import signal
import threading
from concurrent import futures
from pathlib import Path

import grpc
from grpc_health.v1 import health, health_pb2, health_pb2_grpc

from .marking import ALGORITHM, Fingerprint, Marker, UnsupportedImage
from .pb import wm_pb2, wm_pb2_grpc

# Images travel as unary bytes (see wm.proto), so gRPC's 4 MB default is too small.
MAX_MESSAGE_BYTES = 64 * 1024 * 1024
SERVICE = "signunder.wm.v1.WatermarkService"

log = logging.getLogger("wm-worker")

API_CLIENT_NAME = "signunder-api"
"""The name in the API's client certificate (ADR 0010)."""

_TLS_VARS = ("WM_TLS_CERT", "WM_TLS_KEY", "WM_TLS_CLIENT_CA")


def tls_from_env() -> grpc.ServerCredentials | None:
    """Mutual TLS from WM_TLS_CERT, WM_TLS_KEY and WM_TLS_CLIENT_CA (PEM files), or None
    when none is set."""
    paths = [os.environ.get(name) for name in _TLS_VARS]
    if not any(paths):
        return None
    if not all(paths):
        raise SystemExit(f"set all of {', '.join(_TLS_VARS)}, or none")
    cert, key, ca = (Path(path).read_bytes() for path in paths)
    return grpc.ssl_server_credentials(
        [(key, cert)], root_certificates=ca, require_client_auth=True
    )


def _require_api(context) -> None:
    """Refuses any caller whose client certificate does not name the API. Does nothing
    without TLS, where there is no certificate to check."""
    auth = context.auth_context()
    if auth.get("transport_security_type") != [b"ssl"]:
        return
    names = auth.get("x509_subject_alternative_name", [])
    if API_CLIENT_NAME.encode() not in names:
        context.abort(grpc.StatusCode.PERMISSION_DENIED, "this client may not call the worker")


def _fingerprint(fingerprint: Fingerprint) -> wm_pb2.Fingerprint:
    return wm_pb2.Fingerprint(pdq=fingerprint.pdq, quality=fingerprint.quality)


class LazyMarker:
    """Loads the model on a background thread so the port opens at once. On Fly.io a
    stopped machine is started by the first request, and the proxy gives up if the port
    stays closed for more than a few seconds; loading the model takes longer than that on
    a shared CPU. Calls made while it loads wait for it."""

    def __init__(self, load=Marker, wait_seconds: float = 60) -> None:
        self._ready = threading.Event()
        self._marker: Marker | None = None
        self._error: BaseException | None = None
        self._wait_seconds = wait_seconds
        threading.Thread(target=self._load, args=(load,), daemon=True).start()

    def _load(self, load) -> None:
        try:
            self._marker = load()
            log.info("model loaded")
        except BaseException as e:  # reported to every caller rather than lost
            self._error = e
            log.exception("loading the model failed")
        finally:
            self._ready.set()

    def get(self) -> Marker:
        if not self._ready.wait(self._wait_seconds):
            raise ModelNotReady("the watermark model is still loading")
        if self._error is not None:
            raise ModelNotReady(f"the watermark model failed to load: {self._error}")
        return self._marker

    def embed(self, asset, mime_type, payload):
        return self.get().embed(asset, mime_type, payload)

    def decode(self, asset, mime_type):
        return self.get().decode(asset, mime_type)


class ModelNotReady(RuntimeError):
    pass


class WatermarkService(wm_pb2_grpc.WatermarkServiceServicer):
    def __init__(self, marker: Marker | LazyMarker) -> None:
        self._marker = marker

    def Embed(self, request, context):
        _require_api(context)
        try:
            marked, fingerprint = self._marker.embed(
                request.asset, request.mime_type, request.payload
            )
        except ValueError as e:  # includes UnsupportedImage
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(e))
        except ModelNotReady as e:
            context.abort(grpc.StatusCode.UNAVAILABLE, str(e))
        return wm_pb2.EmbedResponse(
            watermarked_asset=marked, fingerprint=_fingerprint(fingerprint), algorithm=ALGORITHM
        )

    def Decode(self, request, context):
        _require_api(context)
        try:
            payload, fingerprint = self._marker.decode(request.asset, request.mime_type)
        except UnsupportedImage as e:
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(e))
        except ModelNotReady as e:
            context.abort(grpc.StatusCode.UNAVAILABLE, str(e))
        return wm_pb2.DecodeResponse(
            present=payload is not None, payload=payload or 0, fingerprint=_fingerprint(fingerprint)
        )


def serve(
    address: str, marker: Marker | LazyMarker, tls: grpc.ServerCredentials | None = None
) -> grpc.Server:
    limits = [
        ("grpc.max_receive_message_length", MAX_MESSAGE_BYTES),
        ("grpc.max_send_message_length", MAX_MESSAGE_BYTES),
    ]
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=4), options=limits)
    wm_pb2_grpc.add_WatermarkServiceServicer_to_server(WatermarkService(marker), server)
    status = health.HealthServicer()
    status.set(SERVICE, health_pb2.HealthCheckResponse.SERVING)
    status.set("", health_pb2.HealthCheckResponse.SERVING)
    health_pb2_grpc.add_HealthServicer_to_server(status, server)
    if tls is None:
        server.add_insecure_port(address)
    else:
        server.add_secure_port(address, tls)
    server.start()
    return server


def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(message)s")
    address = os.environ.get("WM_ADDR", "0.0.0.0:50052")
    tls = tls_from_env()
    marker = LazyMarker()  # the port opens now; the model loads in the background
    server = serve(address, marker, tls)
    if tls is None:
        log.warning("mutual TLS off (WM_TLS_* unset); for local development only")
    else:
        log.info("mutual TLS on: only %s may call", API_CLIENT_NAME)
    log.info("wm-worker listening on %s (%s)", address, ALGORITHM)
    signal.signal(signal.SIGTERM, lambda *_: server.stop(grace=10))
    server.wait_for_termination()


if __name__ == "__main__":
    main()
