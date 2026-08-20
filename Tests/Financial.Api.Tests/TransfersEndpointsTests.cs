using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Financial.Api.Tests;

public class TransfersEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");

    [Fact]
    public async Task AddTransfer_NameStringInGuidField_ReturnsBadRequest()
    {
        var body = new StringContent(
            """{"date":"2026-07-25","sourceBankId":"Barclays","destinationBankId":"Trading212","amount":100}""",
            Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/v1/financial/transfers", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTransfer_ValidRequest_ReturnsOk()
    {
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 500.00m,
            Note = "Round-up top-up"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await response.Content.ReadFromJsonAsync<TransferDTO>();
        transfer.Should().NotBeNull();
        transfer!.SourceBankId.Should().Be(BarclaysId);
        transfer.SourceBankName.Should().Be("Barclays");
        transfer.DestinationBankId.Should().Be(Trading212Id);
        transfer.DestinationBankName.Should().Be("Trading212");
        transfer.Amount.Should().Be(500.00m);
        transfer.Note.Should().Be("Round-up top-up");
    }

    [Fact]
    public async Task AddTransfer_SameSourceAndDestination_ReturnsBadRequest()
    {
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBankId = BarclaysId,
            DestinationBankId = BarclaysId,
            Amount = 100m
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTransfer_NonPositiveAmount_ReturnsBadRequest()
    {
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 0m
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTransfer_UnresolvableBank_ReturnsBadRequest()
    {
        var request = new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            SourceBankId = Guid.NewGuid(),
            DestinationBankId = Trading212Id,
            Amount = 100m
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transfers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransfer_ExistingId_ReturnsOkAndUpdatesFields()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });
        var createdTransfer = await created.Content.ReadFromJsonAsync<TransferDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/transfers/{createdTransfer!.Id}", new TransferUpdateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            SourceBankId = ChaseId,
            DestinationBankId = Trading212Id,
            Amount = 250m,
            Note = "Updated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TransferDTO>();
        updated!.SourceBankId.Should().Be(ChaseId);
        updated.SourceBankName.Should().Be("Chase");
        updated.Amount.Should().Be(250m);
        updated.Note.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateTransfer_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/transfers/{Guid.NewGuid()}", new TransferUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransfer_ExistingId_ReturnsOkAndRemovesTransfer()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });
        var createdTransfer = await created.Content.ReadFromJsonAsync<TransferDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/transfers/{createdTransfer!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await Client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/7");
        list.Should().NotContain(t => t.Id == createdTransfer.Id);
    }

    [Fact]
    public async Task DeleteTransfer_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/transfers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransfersByMonth_ReturnsOnlyTransfersForThatMonth()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });

        var response = await Client.GetAsync("/api/v1/financial/transfers/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransferDTO>>();
        items.Should().ContainSingle();
        items!.Single().Date.Month.Should().Be(7);
    }

    [Fact]
    public async Task GetTransfersByBank_ReturnsTransfersWhereBankIsSourceOrDestination()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 100m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            SourceBankId = ChaseId,
            DestinationBankId = BarclaysId,
            Amount = 50m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 3),
            SourceBankId = ChaseId,
            DestinationBankId = Trading212Id,
            Amount = 25m
        });

        var response = await Client.GetAsync($"/api/v1/financial/transfers/bank/{BarclaysId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransferDTO>>();
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransfersByBank_UnknownBankId_ReturnsEmptyList()
    {
        var response = await Client.GetAsync($"/api/v1/financial/transfers/bank/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransferDTO>>();
        items.Should().BeEmpty();
    }
}
