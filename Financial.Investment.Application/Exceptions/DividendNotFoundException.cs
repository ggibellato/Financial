namespace Financial.Investment.Application.Exceptions;

/// <summary>
/// Thrown when a dividend lookup for a ticker cannot be satisfied — the underlying data source
/// (a web scraper) doesn't reliably distinguish an unknown ticker from a transient failure, so any
/// lookup failure is reported to the caller uniformly as not-found.
/// </summary>
public sealed class DividendNotFoundException : Exception
{
    public DividendNotFoundException(string message) : base(message)
    {
    }
}
