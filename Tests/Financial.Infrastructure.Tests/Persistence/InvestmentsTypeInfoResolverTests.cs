using Financial.Investment.Domain.Entities;
using Financial.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Infrastructure.Tests.Persistence;

public class InvestmentsTypeInfoResolverTests
{
    private static JsonSerializerOptions CreateOptions() => new()
    {
        TypeInfoResolver = new InvestmentsTypeInfoResolver()
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
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_LeavesReadOnlyComputedPropertyUnwired()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Asset), options);

        // PositionType is a computed (get-only) property, not in the excluded list, so it's
        // still present in the JSON output but WirePropertySetter can't find a setter for it.
        var positionTypeProp = typeInfo!.Properties.Should().ContainSingle(p => p.Name == nameof(Asset.PositionType)).Subject;
        positionTypeProp.Set.Should().BeNull();
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
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));

        var json = JsonSerializer.Serialize(asset, options);
        var deserialized = JsonSerializer.Deserialize<Asset>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.Quantity.Should().Be(5m);
        deserialized.AverageSellPrice.Should().Be(110m);
        deserialized.RealizedGainLoss.Should().Be(62m);
    }
}
