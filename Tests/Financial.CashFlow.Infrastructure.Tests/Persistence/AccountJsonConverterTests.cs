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
    private const string V1Bank =
        """{"Id":"5db5c88d-0000-0000-0000-000000000001","Name":"Chase","RoundUpEnabled":true,"OpeningBalance":1250.70,"OpeningBalanceDate":"2026-09-01"}""";

    private readonly JsonSerializerOptions _options = new() { Converters = { new AccountJsonConverter() } };

    [Fact]
    public void Read_V1Bank_BuildsAnActiveGbpBankAccountWithTheSameValues()
    {
        var account = JsonSerializer.Deserialize<Account>(V1Bank, _options)!;

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
    public void ReadThenWrite_V1Bank_ProducesIdenticalBytes()
    {
        var account = JsonSerializer.Deserialize<Account>(V1Bank, _options)!;

        JsonSerializer.Serialize(account, _options).Should().Be(V1Bank);
    }

    [Fact]
    public void Read_V1BankWithoutOpeningBalanceFields_DefaultsToZeroFromTheStartOfTime()
    {
        var account = JsonSerializer.Deserialize<Account>(
            """{"Id":"5db5c88d-0000-0000-0000-000000000002","Name":"Barclays","RoundUpEnabled":false}""", _options)!;

        using (new AssertionScope())
        {
            account.OpeningBalance.Should().Be(OpeningBalance.Create(0m, new DateOnly(1, 1, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(false));
        }
    }

    [Fact]
    public void Write_NewBankAccount_UsesTheV1BankShape()
    {
        var account = Account.Create("Trading212", AccountTemplate.Bank, Currency.GBP);

        var json = JsonSerializer.Serialize(account, _options);

        json.Should().Be(
            $$"""{"Id":"{{account.Id}}","Name":"Trading212","RoundUpEnabled":false,"OpeningBalance":0,"OpeningBalanceDate":"0001-01-01"}""");
    }

    [Fact]
    public void Read_NegativeOpeningBalance_Throws()
    {
        var act = () => JsonSerializer.Deserialize<Account>(
            """{"Id":"5db5c88d-0000-0000-0000-000000000003","Name":"Chase","RoundUpEnabled":true,"OpeningBalance":-1,"OpeningBalanceDate":"2026-09-01"}""",
            _options);

        act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
    }
}
