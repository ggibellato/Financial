using Financial.Investment.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class TransactionEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task AddTransaction_ReturnsOk()
    {
        var request = new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateTime.UtcNow,
            Type = "Buy",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Transactions.Count.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Fee", 0, 0)]
    [InlineData("CapitalCall", 0, 0)]
    [InlineData("ReturnOfCapital", 0, 0)]
    public async Task AddTransaction_NoQuantityEffectType_ReturnsOk(string type, decimal quantity, decimal unitPrice)
    {
        var request = new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateTime.UtcNow,
            Type = type,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = 5
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset!.Transactions.Should().Contain(t => t.Type == type && t.NetCash == -5m);
    }

    [Fact]
    public async Task AddTransaction_TransferInThenTransferOut_ReturnsOk()
    {
        var transferIn = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 5),
            Type = "TransferIn",
            Quantity = 10,
            UnitPrice = 12,
            Fees = 0
        });
        transferIn.StatusCode.Should().Be(HttpStatusCode.OK);

        var transferOut = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 6),
            Type = "TransferOut",
            Quantity = 5,
            UnitPrice = 12,
            Fees = 0
        });

        transferOut.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await transferOut.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset!.Transactions.Should().Contain(t => t.Type == "TransferIn" && t.NetCash == 0m);
        asset.Transactions.Should().Contain(t => t.Type == "TransferOut" && t.NetCash == 0m);
    }

    [Theory]
    [InlineData("Redemption")]
    [InlineData("TransferOut")]
    public async Task AddTransaction_TypeExceedsHeldQuantity_ReturnsBadRequest(string type)
    {
        var buy = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 5),
            Type = "Buy",
            Quantity = 10,
            UnitPrice = 12,
            Fees = 0
        });
        buy.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 6),
            Type = type,
            Quantity = 999,
            UnitPrice = 12,
            Fees = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "an oversell is a well-formed request the domain refuses on its own rules, not a malformed one");
    }

    [Fact]
    public async Task AddTransaction_InvalidType_ReturnsBadRequest()
    {
        var request = new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateTime.UtcNow,
            Type = "NotARealType",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransaction_UnknownId_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/transactions", new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTransaction_UnknownId_ReturnsBadRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/transactions")
        {
            Content = JsonContent.Create(new TransactionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = Guid.NewGuid()
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransaction_ReturnsOk()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var transactionId = createdAsset!.Transactions.First(t => t.Date == new DateTime(2024, 1, 2)).Id;

        var response = await Client.PutAsJsonAsync("/api/v1/financial/transactions", new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = transactionId,
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 2.5m,
            UnitPrice = 12.5m,
            Fees = 1.25m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        var updated = asset!.Transactions.Single(t => t.Id == transactionId);
        updated.Quantity.Should().Be(2.5m);
        updated.UnitPrice.Should().Be(12.5m);
        updated.Fees.Should().Be(1.25m);
    }

    [Fact]
    public async Task DeleteTransaction_ReturnsOk()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 3),
            Type = "Sell",
            Quantity = 1,
            UnitPrice = 15,
            Fees = 0
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var transactionId = createdAsset!.Transactions.First(t => t.Date == new DateTime(2024, 1, 3)).Id;

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/transactions")
        {
            Content = JsonContent.Create(new TransactionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = transactionId
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Transactions.Should().NotContain(t => t.Id == transactionId);
    }

    [Fact]
    public async Task GetTransactionsByBroker_Returns200WithList()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Select(i => i.Date).Should().BeInAscendingOrder();
        items.Should().AllSatisfy(i => i.AssetName.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetTransactionsByBroker_Returns400ForWhitespaceBrokerName()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/broker/%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactionsByPortfolio_Returns200WithList()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/portfolio/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Select(i => i.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetTransactionsByPortfolio_Returns400ForWhitespacePortfolioName()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/portfolio/XPI/%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactionsByPortfolio_Returns400ForWhitespaceBrokerName()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/portfolio/%20/Default");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactionsByBroker_DefaultScope_ExcludesHistoricAssetTransactions()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().OnlyContain(i => i.AssetName == "BCIA11");
    }

    [Fact]
    public async Task GetTransactionsByBroker_ScopeHistoric_ReturnsOnlyHistoricAssetTransactions()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/broker/XPI?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().HaveCount(2);
        items!.Should().OnlyContain(i => i.AssetName == "CLOSEDASSET");
    }

    [Fact]
    public async Task GetTransactionsByPortfolio_ScopeHistoric_ReturnsOnlyHistoricPortfolioTransactions()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/portfolio/XPI/Uncategorized?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().HaveCount(2);
        items!.Should().OnlyContain(i => i.AssetName == "CLOSEDASSET");
    }

    [Fact]
    public async Task GetTransactionTypeEffects_Returns200WithEveryType()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/type-effects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransactionTypeEffectDTO>>();
        items.Should().NotBeNull();
        items!.Select(i => i.Type).Should().BeEquivalentTo(
            ["Buy", "Sell", "Fee", "Redemption", "TransferIn", "TransferOut", "CapitalCall", "ReturnOfCapital"]);
        items.Should().Contain(i => i.Type == "TransferIn" && i.QuantityEffect == "Increase" && i.CashEffect == "None");
        items.Should().Contain(i => i.Type == "Fee" && i.QuantityEffect == "None" && i.CashEffect == "Out");
    }

    [Fact]
    public async Task GetTransactionsByPortfolio_DefaultScope_DoesNotReturnHistoricOnlyPortfolio()
    {
        var response = await Client.GetAsync("/api/v1/financial/transactions/portfolio/XPI/Uncategorized");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<TransactionSummaryItemDTO>>();
        items.Should().NotBeNull();
        items.Should().BeEmpty();
    }
}
