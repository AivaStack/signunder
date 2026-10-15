using System.Numerics;

namespace Signunder.Core.Verification;

/// <summary>Comparing PDQ perceptual hashes, which similar images share most bits of.</summary>
public static class Pdq
{
    public const int HashBytes = 32;

    /// <summary>
    /// The largest number of differing bits (out of 256) still taken as the same image.
    /// Meta's suggested threshold, and the one the robustness suite measures against
    /// (tests/robustness).
    /// </summary>
    public const int MatchDistance = 31;

    /// <summary>
    /// Below this quality PDQ had too little detail to hash (flat or tiny images), so a
    /// close hash proves little. 50 is the cut-off PDQ's authors suggest (from memory; not
    /// re-read).
    /// </summary>
    public const int MinQuality = 50;

    /// <summary>The number of bits in which two hashes differ.</summary>
    public static int Distance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != HashBytes || b.Length != HashBytes)
        {
            throw new ArgumentException($"PDQ hashes are {HashBytes} bytes.");
        }
        var distance = 0;
        for (var i = 0; i < HashBytes; i += sizeof(ulong))
        {
            distance += BitOperations.PopCount(
                BitConverter.ToUInt64(a.Slice(i, sizeof(ulong))) ^ BitConverter.ToUInt64(b.Slice(i, sizeof(ulong))));
        }
        return distance;
    }
}
