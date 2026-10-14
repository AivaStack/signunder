using System.Diagnostics;

namespace Signunder.Api.Signing;

/// <summary>A short reference that ties an error shown to a caller to its entry in the log.</summary>
public static class ErrorReference
{
    public static string New() =>
        Activity.Current?.TraceId.ToHexString()[..16] ?? Guid.NewGuid().ToString("N")[..16];
}
