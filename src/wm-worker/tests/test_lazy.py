import threading
import time

import grpc
import pytest

from wm_worker.marking import Fingerprint
from wm_worker.pb import wm_pb2, wm_pb2_grpc
from wm_worker.server import LazyMarker, ModelNotReady, serve


class SlowStub:
    def decode(self, asset, mime_type):
        return None, Fingerprint(pdq=bytes(32), quality=0)


def test_the_port_opens_before_the_model_has_loaded():
    release = threading.Event()

    def slow_load():
        release.wait(5)
        return SlowStub()

    server = serve("127.0.0.1:50954", LazyMarker(load=slow_load))
    try:
        with grpc.insecure_channel("127.0.0.1:50954") as channel:
            stub = wm_pb2_grpc.WatermarkServiceStub(channel)
            # The call is accepted at once and waits for the model, then completes.
            started = time.monotonic()
            threading.Timer(0.5, release.set).start()
            reply = stub.Decode(
                wm_pb2.DecodeRequest(asset=b"x", mime_type="image/jpeg"), timeout=10
            )
            assert not reply.present
            assert time.monotonic() - started >= 0.4
    finally:
        server.stop(grace=None)


def test_a_model_that_never_loads_is_reported_unavailable():
    def failing_load():
        raise RuntimeError("no weights")

    marker = LazyMarker(load=failing_load, wait_seconds=2)
    with pytest.raises(ModelNotReady, match="no weights"):
        marker.get()

    server = serve("127.0.0.1:50955", marker)
    try:
        with grpc.insecure_channel("127.0.0.1:50955") as channel:
            stub = wm_pb2_grpc.WatermarkServiceStub(channel)
            with pytest.raises(grpc.RpcError) as error:
                stub.Decode(wm_pb2.DecodeRequest(asset=b"x", mime_type="image/jpeg"), timeout=10)
            assert error.value.code() == grpc.StatusCode.UNAVAILABLE
    finally:
        server.stop(grace=None)


def test_a_model_still_loading_after_the_wait_is_reported_unavailable():
    marker = LazyMarker(load=lambda: time.sleep(5), wait_seconds=0.2)
    with pytest.raises(ModelNotReady, match="still loading"):
        marker.get()
