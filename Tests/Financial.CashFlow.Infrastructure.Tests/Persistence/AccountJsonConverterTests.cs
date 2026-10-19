using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Unit")]
public class AccountJsonConverterTests
{
    private const string V2Bank =
        """{"Id":"5db5c88d-0000-0000-0000-000000000001","Name":"Chase","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":1250.70,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":true}}""";

    private readonly JsonSerializerOptions _options = new() { Converters = { new AccountJsonConverter() } };

    [Fact]
    public void Read_V2Bank_BuildsTheAccountWithTheSameValues()
    {
        var account = JsonSerializer.Deserialize<Account>(V2Bank, _options)!;

        using (new AssertionScope())
        {
            account.Id.Should().Be(Guid.Parse("5db5c88d-0000-0000-0000-000000000001"));
            account.Name.Should().Be("Chase");
            account.Template.Should().Be(AccountTemplate.Bank);
            account.Currency.Should().Be(Currency.GBP);
            account.IsActive.Should().BeTrue();
            account.RoundUp.Should().Be(new RoundUpSetting(true));
            account.OpeningBalance.Should().Be(OpeningBalance.Create(1250.70m, new DateOnly(2026, 9, 1)));
        }
    }

    [Fact]
    public void ReadThenWrite_V2Bank_ProducesIdenticalBytes()
    {
        var account = JsonSerializer.Deserialize<Account>(V2Bank, _options)!;

        JsonSerializer.Serialize(account, _options).Should().Be(V2Bank);
    }

    [Fact]
    public void Write_NewBankAccount_UsesTheV2AccountShape()
    {
        var account = Account.Create("Trading212", AccountTemplate.Bank, Currency.GBP);

        var json = JsonSerializer.Serialize(account, _options);

        json.Should().Be(
            $$$"""{"Id":"{{{account.Id}}}","Name":"Trading212","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":0,"StartDate":"0001-01-01"},"RoundUp":{"Enabled":false}}""");
    }

    [Fact]
    public void Read_NullOpeningBalanceAndRoundUp_LeavesBothUnset()
    {
        var account = JsonSerializer.Deserialize<Account>(
            """{"Id":"5db5c88d-0000-0000-0000-000000000002","Name":"Barclays","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null}""",
            _options)!;

        using (new AssertionScope())
        {
            account.OpeningBalance.Should().BeNull();
            account.RoundUp.Should().BeNull();
        }
    }

    [Fact]
    public void Read_NegativeOpeningBalance_Throws()
    {
        var act = () => JsonSerializer.Deserialize<Account>(
            """{"Id":"5db5c88d-0000-0000-0000-000000000003","Name":"Chase","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":-1,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":true}}""",
            _options);

        act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
    }
}
