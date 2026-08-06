using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class InvestmentAccountReferenceConverterTests
{
    private static JsonSerializerOptions CreateOptions(Dictionary<Guid, InvestmentAccount>? lookup) => new()
    {
        Converters = { new InvestmentAccountReferenceConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var lookup = new Dictionary<Guid, InvestmentAccount> { [account.Id] = account };
        var options = CreateOptions(lookup);

        var result = JsonSerializer.Deserialize<InvestmentAccount>($"\"{account.Id}\"", options);

        result.Should().BeSameAs(account);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<InvestmentAccount>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Write_EmitsOnlyTheInvestmentAccountId()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(account, options);

        json.Should().Be($"\"{account.Id}\"");
    }
}
