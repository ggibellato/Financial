using System.Net;

namespace Financial.Integrations.WebPageParser;

public sealed class StatusInvestBlockedException(string message, HttpStatusCode statusCode) : InvalidOperationException(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
