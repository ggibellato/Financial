using System.Net;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Shared.Infrastructure.Resilience;
using FluentAssertions;
using Google;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

public class GoogleTransientErrorTranslatorTests
{
    private static GoogleApiException GoogleApiException(HttpStatusCode statusCode) =>
        new("drive", "Drive error") { HttpStatusCode = statusCode };

    [Fact]
    public void ThrowIfTransient_HttpStatus429_ThrowsTransientStorageExceptionWrappingOriginal()
    {
        var original = GoogleApiException(HttpStatusCode.TooManyRequests);

        var act = () => GoogleTransientErrorTranslator.ThrowIfTransient(original);

        var thrown = act.Should().Throw<TransientStorageException>();
        thrown.Which.InnerException.Should().BeSameAs(original);
    }

    [Fact]
    public void ThrowIfTransient_HttpStatus5xx_ThrowsTransientStorageExceptionWrappingOriginal()
    {
        var original = GoogleApiException(HttpStatusCode.ServiceUnavailable);

        var act = () => GoogleTransientErrorTranslator.ThrowIfTransient(original);

        var thrown = act.Should().Throw<TransientStorageException>();
        thrown.Which.InnerException.Should().BeSameAs(original);
    }

    [Fact]
    public void ThrowIfTransient_HttpStatus400_DoesNotThrow()
    {
        var original = GoogleApiException(HttpStatusCode.BadRequest);

        var act = () => GoogleTransientErrorTranslator.ThrowIfTransient(original);

        act.Should().NotThrow();
    }
}
