using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class BankReferenceConverterTests
{
    private static JsonSerializerOptions CreateOptions(Dictionary<Guid, Bank>? lookup) => new()
    {
        Converters = { new BankReferenceConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var lookup = new Dictionary<Guid, Bank> { [bank.Id] = bank };
        var options = CreateOptions(lookup);

        var result = JsonSerializer.Deserialize<Bank>($"\"{bank.Id}\"", options);

        result.Should().BeSameAs(bank);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<Bank>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var options = CreateOptions([]);

        var result = JsonSerializer.Deserialize<Bank>("null", options);

        result.Should().BeNull();
    }

    [Fact]
    public void Write_EmitsOnlyTheBankId()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(bank, options);

        json.Should().Be($"\"{bank.Id}\"");
    }
}
