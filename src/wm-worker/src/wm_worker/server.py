"""gRPC server for the watermark worker."""

import logging
import os
import signal
from concurrent import futures

import grpc
from grpc_health.v1 import health, health_pb2, health_pb2_grpc

from .marking import ALGORITHM, Fingerprint, Marker, UnsupportedImage
from .pb import wm_pb2, wm_pb2_grpc

# Images travel as unary bytes (see wm.proto), so gRPC's 4 MB default is too small.
MAX_MESSAGE_BYTES = 64 * 1024 * 1024
SERVICE = "signunder.wm.v1.WatermarkService"

log = logging.getLogger("wm-worker")


def _fingerprint(fingerprint: Fingerprint) -> wm_pb2.Fingerprint:
    return wm_pb2.Fingerprint(pdq=fingerprint.pdq, quality=fingerprint.quality)


class WatermarkService(wm_pb2_grpc.WatermarkServiceServicer):
    def __init__(self, marker: Marker) -> None:
        self._marker = marker

    def Embed(self, request, context):
        try:
            marked, fingerprint = self._marker.embed(
                request.asset, request.mime_type, request.payload
            )
        except ValueError as e:  # includes UnsupportedImage
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(e))
        return wm_pb2.EmbedResponse(
            watermarked_asset=marked, fingerprint=_fingerprint(fingerprint), algorithm=ALGORITHM
        )

    def Decode(self, request, context):
        try:
            payload, fingerprint = self._marker.decode(request.asset, request.mime_type)
        except UnsupportedImage as e:
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(e))
        return wm_pb2.DecodeResponse(
            present=payload is not None, payload=payload or 0, fingerprint=_fingerprint(fingerprint)
        )


def serve(address: str, marker: Marker) -> grpc.Server:
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
    server.add_insecure_port(address)
    server.start()
    return server


def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(message)s")
    address = os.environ.get("WM_ADDR", "0.0.0.0:50052")
    marker = Marker()  # loads the model before the port opens
    server = serve(address, marker)
    log.info("wm-worker listening on %s (%s)", address, ALGORITHM)
    signal.signal(signal.SIGTERM, lambda *_: server.stop(grace=10))
    server.wait_for_termination()


if __name__ == "__main__":
    main()
