using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class PaymentsDueEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetPaymentsDue_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/payments-due");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDueDTO>>();
        payments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentsDue_NoQualifyingPayments_ReturnsEmptyArray()
    {
        var response = await Client.GetAsync("/api/v1/financial/payments-due");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDueDTO>>();
        payments.Should().BeEmpty();
    }
}
