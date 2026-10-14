from pathlib import Path

import grpc
import pytest

from wm_worker.pb import wm_pb2, wm_pb2_grpc
from wm_worker.server import serve

DEV = Path(__file__).parents[3] / "deploy" / "internal-tls" / "dev"
PHOTO = Path(__file__).parents[3] / "tests" / "robustness" / "images" / "earth.jpg"
ADDRESS = "localhost:50953"


def read(name: str) -> bytes:
    return (DEV / name).read_bytes()


class StubMarker:
    """Stands in for the model: TLS is what is under test."""

    def decode(self, asset, mime_type):
        from wm_worker.marking import Fingerprint

        return None, Fingerprint(pdq=bytes(32), quality=0)


@pytest.fixture(scope="module")
def server():
    tls = grpc.ssl_server_credentials(
        [(read("wm-worker.key"), read("wm-worker.pem"))],
        root_certificates=read("ca.pem"),
        require_client_auth=True,
    )
    running = serve(ADDRESS, StubMarker(), tls)
    yield
    running.stop(grace=None)


def decode(client: str | None):
    credentials = grpc.ssl_channel_credentials(
        root_certificates=read("ca.pem"),
        private_key=read(f"{client}.key") if client else None,
        certificate_chain=read(f"{client}.pem") if client else None,
    )
    with grpc.secure_channel(ADDRESS, credentials) as channel:
        stub = wm_pb2_grpc.WatermarkServiceStub(channel)
        return stub.Decode(wm_pb2.DecodeRequest(asset=b"x", mime_type="image/jpeg"), timeout=5)


def test_the_api_certificate_is_accepted(server):
    assert not decode("signunder-api").present


def test_a_call_without_a_client_certificate_is_refused(server):
    with pytest.raises(grpc.RpcError) as error:
        decode(None)
    assert error.value.code() == grpc.StatusCode.UNAVAILABLE


def test_a_server_certificate_is_refused(server):
    # Same CA, but a server certificate: refused at the handshake.
    with pytest.raises(grpc.RpcError) as error:
        decode("sign-sidecar")
    assert error.value.code() == grpc.StatusCode.UNAVAILABLE


def test_a_client_certificate_for_someone_else_is_refused_by_name(server):
    # A valid client certificate from the same CA that does not name the API: the
    # handshake succeeds, so this is the worker's name check refusing it.
    with pytest.raises(grpc.RpcError) as error:
        decode("other-client")
    assert error.value.code() == grpc.StatusCode.PERMISSION_DENIED
