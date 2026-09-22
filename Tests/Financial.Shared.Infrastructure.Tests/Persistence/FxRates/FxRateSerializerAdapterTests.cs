using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

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
        var record = new FxRateRecord("USD", 5.452317m, 0.771845m, "frankfurter", new DateTimeOffset(2026, 9, 18, 23, 59, 59, TimeSpan.Zero));
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord> { [new DateOnly(2026, 9, 18)] = record };

        var json = Serializer.Serialize(ratesByDate);

        json.Should().Contain("\"2026-09-18\"");
        json.Should().Contain("\"base\":\"USD\"");
        json.Should().Contain("\"BRL\":5.452317");
        json.Should().Contain("\"GBP\":0.771845");
        json.Should().Contain("\"source\":\"frankfurter\"");
    }

    [Fact]
    public void Deserialize_Missing_Version_Treated_As_Version_1()
    {
        var json = "{\"ratesByDate\":{}}";

        var act = () => Serializer.Deserialize(json);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deserialize_Missing_RatesByDate_Returns_Empty_Dictionary()
    {
        var result = Serializer.Deserialize("{\"Version\":1}");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Round_Trip_Preserves_At_Least_Six_Decimal_Places()
    {
        var record = new FxRateRecord("USD", 5.452317m, 0.771845m, "frankfurter", DateTimeOffset.UtcNow);
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord> { [new DateOnly(2026, 9, 18)] = record };

        var json = Serializer.Serialize(ratesByDate);
        var result = Serializer.Deserialize(json);

        result[new DateOnly(2026, 9, 18)].BrlRate.Should().Be(5.452317m);
        result[new DateOnly(2026, 9, 18)].GbpRate.Should().Be(0.771845m);
    }

    [Fact]
    public void Round_Trip_Preserves_Source_And_StoredAt()
    {
        var storedAt = new DateTimeOffset(2026, 9, 18, 23, 59, 59, TimeSpan.Zero);
        var record = new FxRateRecord("USD", 5.452317m, 0.771845m, "frankfurter", storedAt);
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord> { [new DateOnly(2026, 9, 18)] = record };

        var json = Serializer.Serialize(ratesByDate);
        var result = Serializer.Deserialize(json);

        result[new DateOnly(2026, 9, 18)].Source.Should().Be("frankfurter");
        result[new DateOnly(2026, 9, 18)].StoredAt.Should().Be(storedAt);
    }

    [Fact]
    public void Round_Trip_Multiple_Dates_Preserves_All_Entries()
    {
        var ratesByDate = new Dictionary<DateOnly, FxRateRecord>
        {
            [new DateOnly(2026, 9, 17)] = new FxRateRecord("USD", 5.40m, 0.77m, "frankfurter", DateTimeOffset.UtcNow),
            [new DateOnly(2026, 9, 18)] = new FxRateRecord("USD", 5.45m, 0.78m, "frankfurter", DateTimeOffset.UtcNow)
        };

        var json = Serializer.Serialize(ratesByDate);
        var result = Serializer.Deserialize(json);

        result.Should().HaveCount(2);
        result.Should().ContainKey(new DateOnly(2026, 9, 17));
        result.Should().ContainKey(new DateOnly(2026, 9, 18));
    }
}
