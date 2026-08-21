namespace Financial.Investment.Application.Exceptions;

/// <summary>
/// Thrown when no registered price source supports an asset's class, so no lookup is attempted.
/// <para>
/// Distinct from a lookup that was attempted and failed: nothing is wrong with the request or the
/// provider, the holding simply has no price source. Reporting it as a server error made a
/// permanent, expected condition look like an outage the user should retry.
/// </para>
/// </summary>
public sealed class UnsupportedAssetClassException : Exception
{
    public UnsupportedAssetClassException(string message) : base(message)
    {
    }
}
