using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CreditCardReferenceConverterTests
{
    private static JsonSerializerOptions CreateOptions(Dictionary<Guid, CreditCard>? lookup) => new()
    {
        Converters = { new CreditCardReferenceConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var card = CreditCard.Create("BarclaysPlatinumVisa8003");
        var lookup = new Dictionary<Guid, CreditCard> { [card.Id] = card };
        var options = CreateOptions(lookup);

        var result = JsonSerializer.Deserialize<CreditCard>($"\"{card.Id}\"", options);

        result.Should().BeSameAs(card);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<CreditCard>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var options = CreateOptions([]);

        var result = JsonSerializer.Deserialize<CreditCard>("null", options);

        result.Should().BeNull();
    }

    [Fact]
    public void Write_EmitsOnlyTheCardId()
    {
        var card = CreditCard.Create("BarclaysPlatinumVisa8003");
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(card, options);

        json.Should().Be($"\"{card.Id}\"");
    }
}
