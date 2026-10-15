using Signunder.Core.Verification;

namespace Signunder.Core.Tests;

public class PdqTests
{
    private static readonly byte[] Hash = [.. Enumerable.Range(0, 32).Select(i => (byte)(i * 7))];

    [Fact]
    public void Distance_counts_the_differing_bits()
    {
        var near = (byte[])Hash.Clone();
        near[0] ^= 0b1000_0001;
        near[31] ^= 0b0001_0000;

        Assert.Equal(0, Pdq.Distance(Hash, Hash));
        Assert.Equal(3, Pdq.Distance(Hash, near));
        Assert.Equal(256, Pdq.Distance(Hash, [.. Hash.Select(b => (byte)~b)]));
    }

    [Fact]
    public void Distance_refuses_anything_but_256_bit_hashes()
    {
        Assert.Throws<ArgumentException>(() => Pdq.Distance(Hash, Hash.AsSpan(0, 31)));
    }
}
