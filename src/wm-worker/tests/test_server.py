from pathlib import Path

import grpc
import pytest

from wm_worker.marking import ALGORITHM, Marker
from wm_worker.pb import wm_pb2, wm_pb2_grpc
from wm_worker.server import MAX_MESSAGE_BYTES, serve

PHOTO = Path(__file__).parents[3] / "tests" / "robustness" / "images" / "earth.jpg"


@pytest.fixture(scope="module")
def stub():
    server = serve("127.0.0.1:50952", Marker())
    limits = [
        ("grpc.max_receive_message_length", MAX_MESSAGE_BYTES),
        ("grpc.max_send_message_length", MAX_MESSAGE_BYTES),
    ]
    with grpc.insecure_channel("127.0.0.1:50952", options=limits) as channel:
        yield wm_pb2_grpc.WatermarkServiceStub(channel)
    server.stop(grace=None)


def test_embed_then_decode(stub):
    photo = PHOTO.read_bytes()
    payload = (1 << 61) - 12345

    embedded = stub.Embed(wm_pb2.EmbedRequest(asset=photo, mime_type="image/jpeg", payload=payload))

    assert embedded.algorithm == ALGORITHM
    assert len(embedded.fingerprint.pdq) == 32
    decoded = stub.Decode(
        wm_pb2.DecodeRequest(asset=embedded.watermarked_asset, mime_type="image/jpeg")
    )
    assert decoded.present
    assert decoded.payload == payload


def test_decode_of_an_unmarked_image(stub):
    decoded = stub.Decode(wm_pb2.DecodeRequest(asset=PHOTO.read_bytes(), mime_type="image/jpeg"))

    assert not decoded.present
    assert decoded.payload == 0
    assert len(decoded.fingerprint.pdq) == 32


@pytest.mark.parametrize(
    "request_",
    [
        wm_pb2.EmbedRequest(asset=b"nope", mime_type="image/jpeg", payload=1),
        wm_pb2.EmbedRequest(asset=PHOTO.read_bytes(), mime_type="image/jpeg", payload=1 << 61),
    ],
)
def test_bad_requests_are_invalid_arguments(stub, request_):
    with pytest.raises(grpc.RpcError) as error:
        stub.Embed(request_)

    assert error.value.code() == grpc.StatusCode.INVALID_ARGUMENT
