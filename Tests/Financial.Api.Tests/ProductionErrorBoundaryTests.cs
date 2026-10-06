using System.Net;
using Financial.Shared.Abstractions.Resilience;
using FluentAssertions;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ProductionErrorBoundaryTests
{
    private const string BanksPath = "/api/v1/financial/banks";
    private const string LeakyMessage = "Ledger for Ariana is off by 1234.56";

    private static async Task<HttpResponseMessage> GetBanksFailingWith(Func<Exception> exception)
    {
        await using var factory = new ApiTestFactory(
            environment: "Production",
            configureServices: ThrowingBankService.Registering(exception));
        using var client = factory.CreateClient();
        return await client.GetAsync(BanksPath);
    }

    [Fact]
    public async Task UnmappedException_Returns500ProblemDetails()
    {
        var response = await GetBanksFailingWith(() => new InvalidOperationException(LeakyMessage));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"status\":500");
    }

    [Fact]
    public async Task UnmappedException_LeaksNothing()
    {
        var response = await GetBanksFailingWith(() => new InvalidOperationException(LeakyMessage));

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("Ariana")
            .And.NotContain("1234.56")
            .And.NotContain(nameof(InvalidOperationException))
            .And.NotContain("Financial.")
            .And.NotContain("   at ")
            .And.NotContainEquivalentOf("stackTrace")
            .And.NotContainEquivalentOf("\"exception\"");
    }

    [Fact]
    public async Task TransientStorageException_Returns503WithRetryAfter()
    {
        var response = await GetBanksFailingWith(
            () => new TransientStorageException("Drive request failed", new InvalidOperationException()));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter?.Delta.Should().Be(TimeSpan.FromSeconds(30));
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
