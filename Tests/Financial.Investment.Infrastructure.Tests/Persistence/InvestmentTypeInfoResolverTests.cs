using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Investment.Infrastructure.Tests.Persistence;

public class InvestmentTypeInfoResolverTests
{
    private static JsonSerializerOptions CreateOptions() => new()
    {
        TypeInfoResolver = new InvestmentTypeInfoResolver()
    };

    [Fact]
    public void GetTypeInfo_ForManagedType_EnablesPrivateConstructor()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Asset), options);

        typeInfo!.CreateObject.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_RemovesExcludedProperties()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Asset), options);

        typeInfo!.Properties.Should().NotContain(p => p.Name == nameof(Asset.AveragePrice));
        typeInfo.Properties.Should().NotContain(p => p.Name == nameof(Asset.Quantity));
        typeInfo.Properties.Should().NotContain(p => p.Name == nameof(Asset.AverageSellPrice));
        typeInfo.Properties.Should().NotContain(p => p.Name == nameof(Asset.RealizedGainLoss));
        typeInfo.Properties.Should().NotContain(p => p.Name == nameof(Asset.PositionType));
    }

    [Fact]
    public void GetTypeInfo_ForPortfolio_RemovesIsEmpty()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Portfolio), options);

        typeInfo!.Properties.Should().NotContain(p => p.Name == nameof(Portfolio.IsEmpty));
    }

    [Fact]
    public void GetTypeInfo_DeserializesAssetJsonStillContainingPositionType_LoadsCleanly()
    {
        // UnmappedMemberHandling defaults to Skip, so a still-present legacy key is ignored.
        var options = CreateOptions();
        const string legacyJson = """
            {
                "Name": "Test",
                "ISIN": "ISIN",
                "Exchange": "BVMF",
                "Ticker": "TST",
                "Country": 0,
                "LocalTypeCode": "",
                "Class": 0,
                "PositionType": "Long",
                "Transactions": [],
                "Credits": []
            }
            """;

        var deserialized = JsonSerializer.Deserialize<Asset>(legacyJson, options);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test");
    }

    [Fact]
    public void GetTypeInfo_DeserializesPortfolioJsonStillContainingIsEmpty_LoadsCleanly()
    {
        var options = CreateOptions();
        const string legacyJson = """
            {
                "Name": "Test Portfolio",
                "IsEmpty": false,
                "Assets": []
            }
            """;

        var deserialized = JsonSerializer.Deserialize<Portfolio>(legacyJson, options);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test Portfolio");
        deserialized.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_WiresSettableProperties()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Asset), options);

        var nameProp = typeInfo!.Properties.Should().ContainSingle(p => p.Name == nameof(Asset.Name)).Subject;
        nameProp.Set.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_ForUnmanagedType_ReturnsUnmodifiedTypeInfo()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(string), options);

        typeInfo.Should().NotBeNull();
        typeInfo!.Kind.Should().Be(JsonTypeInfoKind.None);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsAssetThroughPrivateConstructorAndExcludedProperties()
    {
        // End-to-end: serializing then deserializing an Asset recomputes AveragePrice/Quantity
        // from transactions rather than trusting the excluded JSON fields directly.
        var options = CreateOptions();
        var asset = Asset.Create("Test", "ISIN", "BVMF", "TST");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 5m, 10m, 0m));

        var json = JsonSerializer.Serialize(asset, options);
        var deserialized = JsonSerializer.Deserialize<Asset>(json, options);

        json.Should().NotContain(nameof(Asset.PositionType));
        deserialized.Should().NotBeNull();
        deserialized!.Quantity.Should().Be(5m);
        deserialized.AveragePrice.Should().Be(10m);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsAssetWithSellTransaction_RecalculatesRealizedGainLossAndAverageSellPrice()
    {
        // End-to-end: Transactions is a plain JSON array on disk (ICollection<Transaction>),
        // and RealizedGainLoss/AverageSellPrice recompute from it on deserialize rather than
        // trusting the excluded JSON fields directly.
        var options = CreateOptions();
        var asset = Asset.Create("Test", "ISIN", "BVMF", "TST");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));

        var json = JsonSerializer.Serialize(asset, options);
        var deserialized = JsonSerializer.Deserialize<Asset>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.Quantity.Should().Be(5m);
        deserialized.AverageSellPrice.Should().Be(110m);
        deserialized.RealizedGainLoss.Should().Be(62m);
    }

    [Fact]
    public void GetTypeInfo_ForAssetPriceSnapshot_EnablesPrivateConstructor()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(AssetPriceSnapshot), options);

        typeInfo!.CreateObject.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_RoundTripsAssetWithPriceSnapshots_PreservesEntries()
    {
        var options = CreateOptions();
        var asset = Asset.Create("Test", "ISIN", "BVMF", "TST");
        asset.SetPrice(new DateOnly(2026, 8, 14), 100m, isManual: false);
        asset.SetPrice(new DateOnly(2026, 8, 15), 105m, isManual: true);

        var json = JsonSerializer.Serialize(asset, options);
        var deserialized = JsonSerializer.Deserialize<Asset>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.PriceSnapshots.Should().HaveCount(2);
        var manualEntry = deserialized.GetPriceForDate(new DateOnly(2026, 8, 15));
        manualEntry.Should().NotBeNull();
        manualEntry!.Price.Should().Be(105m);
        manualEntry.IsManual.Should().BeTrue();
    }

    [Fact]
    public void GetTypeInfo_DeserializesAssetJsonWithoutPriceSnapshotsProperty_LoadsAsEmptyCollection()
    {
        // Simulates a data file written before this feature existed: no "PriceSnapshots"
        // property at all, not even an empty array.
        var options = CreateOptions();
        const string legacyJson = """
            {
                "Name": "Test",
                "ISIN": "ISIN",
                "Exchange": "BVMF",
                "Ticker": "TST",
                "Country": 0,
                "LocalTypeCode": "",
                "Class": 0,
                "Transactions": [],
                "Credits": []
            }
            """;

        var deserialized = JsonSerializer.Deserialize<Asset>(legacyJson, options);

        deserialized.Should().NotBeNull();
        deserialized!.PriceSnapshots.Should().BeEmpty();
    }

    [Fact]
    public void GetTypeInfo_DeserializesBrokerJsonWithoutCostBasisMethodProperty_DefaultsToAverageCost()
    {
        // Simulates a data file written before this feature existed: no "CostBasisMethod"
        // property at all, not even an explicit "AverageCost" entry.
        var options = CreateOptions();
        const string legacyJson = """
            {
                "Name": "Broker A",
                "Currency": "USD",
                "Portfolios": []
            }
            """;

        var deserialized = JsonSerializer.Deserialize<Broker>(legacyJson, options);

        deserialized.Should().NotBeNull();
        deserialized!.CostBasisMethod.Should().Be(CostBasisMethod.AverageCost);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsBrokerCostBasisMethod()
    {
        var options = CreateOptions();
        var broker = Broker.Create("Broker A", "USD");
        broker.SetCostBasisMethod(CostBasisMethod.FIFO);

        var json = JsonSerializer.Serialize(broker, options);
        var deserialized = JsonSerializer.Deserialize<Broker>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
    }

    [Fact]
    public void GetTypeInfo_ForCorporateAction_EnablesPrivateConstructor()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(CorporateAction), options);

        typeInfo!.CreateObject.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_RoundTripsAssetWithCorporateAction_PreservesFields()
    {
        var options = CreateOptions();
        var asset = Asset.Create("Test", "ISIN", "BVMF", "TST");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2024, 6, 1), 2.0m, "2-for-1 split"));

        var json = JsonSerializer.Serialize(asset, options);
        var deserialized = JsonSerializer.Deserialize<Asset>(json, options);

        deserialized.Should().NotBeNull();
        var corporateAction = deserialized!.CorporateActions.Should().ContainSingle().Subject;
        corporateAction.Type.Should().Be(CorporateAction.CorporateActionType.Split);
        corporateAction.EffectiveDate.Should().Be(new DateTime(2024, 6, 1));
        corporateAction.RatioFactor.Should().Be(2.0m);
        corporateAction.Note.Should().Be("2-for-1 split");
        deserialized.Quantity.Should().Be(20m, "the round-tripped asset must still reflect the split applied during replay");
    }

    [Fact]
    public void GetTypeInfo_DeserializesAssetJsonWithoutCorporateActionsProperty_LoadsAsEmptyCollection()
    {
        // Simulates a data file written before this feature existed: no "CorporateActions"
        // property at all, not even an empty array.
        var options = CreateOptions();
        const string legacyJson = """
            {
                "Name": "Test",
                "ISIN": "ISIN",
                "Exchange": "BVMF",
                "Ticker": "TST",
                "Country": 0,
                "LocalTypeCode": "",
                "Class": 0,
                "Transactions": [],
                "Credits": []
            }
            """;

        var deserialized = JsonSerializer.Deserialize<Asset>(legacyJson, options);

        deserialized.Should().NotBeNull();
        deserialized!.CorporateActions.Should().BeEmpty();
    }
}
