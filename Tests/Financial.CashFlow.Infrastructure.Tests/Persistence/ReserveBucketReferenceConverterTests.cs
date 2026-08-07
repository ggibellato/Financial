using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class ReserveBucketReferenceConverterTests
{
    private static JsonSerializerOptions CreateOptions(Dictionary<Guid, ReserveBucket>? lookup) => new()
    {
        Converters = { new ReserveBucketReferenceConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m);
        var lookup = new Dictionary<Guid, ReserveBucket> { [bucket.Id] = bucket };
        var options = CreateOptions(lookup);

        var result = JsonSerializer.Deserialize<ReserveBucket>($"\"{bucket.Id}\"", options);

        result.Should().BeSameAs(bucket);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<ReserveBucket>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var options = CreateOptions([]);

        var result = JsonSerializer.Deserialize<ReserveBucket>("null", options);

        result.Should().BeNull();
    }

    [Fact]
    public void Write_EmitsOnlyTheBucketId()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m);
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(bucket, options);

        json.Should().Be($"\"{bucket.Id}\"");
    }
}
