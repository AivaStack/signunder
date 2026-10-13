namespace Signunder.Core.Verification;

/// <summary>c2pa-rs's overall verdict on a manifest store.</summary>
public enum ValidationState
{
    /// <summary>Not well formed, or a cryptographic check failed.</summary>
    Invalid,

    /// <summary>Well formed and intact, but the signer is not on the trust list.</summary>
    Valid,

    /// <summary>Valid, and the signing certificate chains to the trust list.</summary>
    Trusted,
}
