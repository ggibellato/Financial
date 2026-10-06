using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ProductionErrorBoundaryTests(ProductionThrowingApiHost host) : IClassFixture<ProductionThrowingApiHost>
{
    [Fact]
    public async Task UnmappedException_Returns500ProblemDetailsThatLeaksNothing()
    {
        var response = await host.GetBanksFailingWith(new InvalidOperationException("Ledger for Ariana is off by 1234.56"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(500);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Ariana")
            .And.NotContain("1234.56")
            .And.NotContain(nameof(InvalidOperationException))
            .And.NotContain("Financial.")
            .And.NotContain("   at ")
            .And.NotContainEquivalentOf("stackTrace")
            .And.NotContainEquivalentOf("\"exception\"");
    }
}
