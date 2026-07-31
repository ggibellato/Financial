using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class TransfersEndpointsTests
{
    [Fact]
    public async Task AddTransfer_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 500.00m,
            Note = "Round-up top-up"
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await response.Content.ReadFromJsonAsync<TransferDTO>();
        transfer.Should().NotBeNull();
        transfer!.SourceBank.Should().Be("Barclays");
        transfer.DestinationBank.Should().Be("Trading212");
        transfer.Amount.Should().Be(500.00m);
        transfer.Note.Should().Be("Round-up top-up");
    }

    [Fact]
    public async Task AddTransfer_SameSourceAndDestination_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBank = "Barclays",
            DestinationBank = "Barclays",
            Amount = 100m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTransfer_NonPositiveAmount_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 0m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTransfer_UnresolvableBank_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBank = "NotABank",
            DestinationBank = "Trading212",
            Amount = 100m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransfer_ExistingId_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });
        var createdTransfer = await created.Content.ReadFromJsonAsync<TransferDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/transfers/{createdTransfer!.Id}", new TransferUpdateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            SourceBank = "Chase",
            DestinationBank = "Trading212",
            Amount = 250m,
            Note = "Updated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TransferDTO>();
        updated!.SourceBank.Should().Be("Chase");
        updated.Amount.Should().Be(250m);
        updated.Note.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateTransfer_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/transfers/{Guid.NewGuid()}", new TransferUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransfer_ExistingId_ReturnsOkAndRemovesTransfer()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });
        var createdTransfer = await created.Content.ReadFromJsonAsync<TransferDTO>();

        var response = await client.DeleteAsync($"/api/v1/financial/transfers/{createdTransfer!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/7");
        list.Should().NotContain(t => t.Id == createdTransfer.Id);
    }

    [Fact]
    public async Task DeleteTransfer_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/transfers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransfersByMonth_ReturnsOnlyTransfersForThatMonth()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });

        var response = await client.GetAsync("/api/v1/financial/transfers/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransferDTO>>();
        items.Should().ContainSingle();
        items!.Single().Date.Month.Should().Be(7);
    }

    [Fact]
    public async Task GetTransfersByBank_ReturnsTransfersWhereBankIsSourceOrDestination()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 100m
        });
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            SourceBank = "Chase",
            DestinationBank = "Barclays",
            Amount = 50m
        });
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 3),
            SourceBank = "Chase",
            DestinationBank = "Trading212",
            Amount = 25m
        });

        var response = await client.GetAsync("/api/v1/financial/transfers/bank/Barclays");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransferDTO>>();
        items.Should().HaveCount(2);
    }
}
