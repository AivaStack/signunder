using Grpc.Core;
using Signunder.Api.Signing;

namespace Signunder.Api.Tests;

public class ReconnectTests
{
    private static RpcException Transport(StatusCode code) =>
        new(new Status(code, "connection broke", new HttpRequestException("ping timed out")));

    [Fact]
    public async Task A_transport_failure_is_retried_on_a_new_call()
    {
        var calls = 0;

        var result = await InternalTls.WithReconnect(() =>
            ++calls < 3 ? throw Transport(StatusCode.Internal) : Task.FromResult("ok"));

        Assert.Equal("ok", result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task It_gives_up_after_three_attempts()
    {
        var calls = 0;

        await Assert.ThrowsAsync<RpcException>(() =>
            InternalTls.WithReconnect<string>(() => { calls++; throw Transport(StatusCode.Unavailable); }));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task A_failure_inside_the_service_is_not_retried()
    {
        var calls = 0;

        await Assert.ThrowsAsync<RpcException>(() =>
            InternalTls.WithReconnect<string>(() =>
            {
                calls++;
                throw new RpcException(new Status(StatusCode.Internal, "c2pa: signing failed"));
            }));

        Assert.Equal(1, calls);
    }
}
