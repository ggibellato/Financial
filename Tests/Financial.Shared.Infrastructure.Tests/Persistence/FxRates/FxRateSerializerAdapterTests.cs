using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

[Trait("Category", "Unit")]
public class FxRateSerializerAdapterTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    [Fact]
    public void Serialize_Stamps_Current_Version()
    {
        var json = Serializer.Serialize(new Dictionary<DateOnly, FxRateRecord>());

        json.Should().Contain("\"Version\":1");
    }

    [Fact]
    public void Serialize_Writes_Base_Rates_Source_And_StoredAt()
    {
        var record = new FxRateRecord(5.452317m, 0.771845m, "frankfurter", new DateTimeOffset(2026, 9, 18, 23, 59, 59, TimeSpan.Zero));
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord> { [new DateOnly(2026, 9, 18)] = record };

        var json = Serializer.Serialize(ratesByDate);

        json.Should().Contain("\"2026-09-18\"");
        json.Should().Contain("\"base\":\"USD\"");
        json.Should().Contain("\"BRL\":5.452317");
        json.Should().Contain("\"GBP\":0.771845");
        json.Should().Contain("\"source\":\"frankfurter\"");
        json.Should().Contain("\"storedAt\":\"2026-09-18T23:59:59.0000000");
    }

    [Fact]
    public void Deserialize_Missing_RatesByDate_Returns_Empty_Dictionary()
    {
        var result = Serializer.Deserialize("{\"Version\":1}");

        result.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_KeepsEveryDateWithSixDecimalRatesSourceAndStoredAt()
    {
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord>
        {
            [new DateOnly(2026, 9, 17)] = new FxRateRecord(5.40m, 0.77m, "manual", TestClock.Default),
            [new DateOnly(2026, 9, 18)] = new FxRateRecord(5.452317m, 0.771845m, "frankfurter",
                new DateTimeOffset(2026, 9, 18, 23, 59, 59, TimeSpan.Zero))
        };

        var result = Serializer.Deserialize(Serializer.Serialize(ratesByDate));

        result.Should().BeEquivalentTo(ratesByDate);
    }
}
