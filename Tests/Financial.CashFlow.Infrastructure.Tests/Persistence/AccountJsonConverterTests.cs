using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Unit")]
public class AccountJsonConverterTests
{
    private const string V4Bank =
        """{"Id":"5db5c88d-0000-0000-0000-000000000001","Name":"Chase","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":1250.70,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":true},"Pots":null,"CategoryId":null}""";

    private const string ReservaCategoryId = "5db5c88d-0000-0000-0000-000000000007";

    private const string Reserva =
        $$"""{"Id":"5db5c88d-0000-0000-0000-000000000004","Name":"Reserva","Template":"Pots","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null,"Pots":[{"Id":"5db5c88d-0000-0000-0000-000000000005","Name":"Investimento","IsActive":true,"SplitPercentage":33.33},{"Id":"5db5c88d-0000-0000-0000-000000000006","Name":"Ariana","IsActive":false,"SplitPercentage":0}],"CategoryId":"{{ReservaCategoryId}}"}""";

    private const string Mae =
        """{"Id":"5db5c88d-0000-0000-0000-000000000008","Name":"Mãe","Template":"Settlement","Currency":"BRL","IsActive":true,"OpeningBalance":null,"RoundUp":null,"Pots":null,"CategoryId":null,"Currencies":["BRL","GBP"],"OtherParticipant":"Mãe"}""";

    private readonly JsonSerializerOptions _options;

    public AccountJsonConverterTests()
    {
        var context = new ReferenceResolutionContext();
        var category = JsonSerializer.Deserialize<Category>(
            $$"""{"Id":"{{ReservaCategoryId}}","Name":"Reserva","Active":true,"IsInvestment":false,"IsTithe":false}""", CashFlowElementOptions.Create())!;
        context.Categories[category.Id] = category;
        _options = new JsonSerializerOptions
        {
            Converters = { new AccountJsonConverter(context) },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    [Fact]
    public void Read_V4Bank_BuildsTheAccountWithTheSameValues()
    {
        var account = JsonSerializer.Deserialize<Account>(V4Bank, _options)!;

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
    public void ReadThenWrite_V4Bank_ProducesIdenticalBytes()
    {
        var account = JsonSerializer.Deserialize<Account>(V4Bank, _options)!;

        JsonSerializer.Serialize(account, _options).Should().Be(V4Bank);
    }

    [Fact]
    public void Write_NewBankAccount_UsesTheV4AccountShape()
    {
        var account = Account.Create("Trading212", AccountTemplate.Bank, Currency.GBP);

        var json = JsonSerializer.Serialize(account, _options);

        json.Should().Be(
            $$$"""{"Id":"{{{account.Id}}}","Name":"Trading212","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":0,"StartDate":"0001-01-01"},"RoundUp":{"Enabled":false},"Pots":null,"CategoryId":null}""");
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
    public void ReadThenWrite_PotsAccount_KeepsItsPotsInOrderAndItsCategory()
    {
        var account = JsonSerializer.Deserialize<Account>(Reserva, _options)!;

        using (new AssertionScope())
        {
            account.Pots!.Select(pot => (pot.Name, pot.IsActive, pot.SplitPercentage)).Should().Equal(("Investimento", true, 33.33m), ("Ariana", false, 0m));
            account.OwnsPot(Guid.Parse("5db5c88d-0000-0000-0000-000000000006")).Should().BeTrue();
            account.Category!.Id.Should().Be(Guid.Parse(ReservaCategoryId));
            JsonSerializer.Serialize(account, _options).Should().Be(Reserva);
        }
    }

    [Fact]
    public void Read_PotsAccountWithoutCategory_LeavesItUnset()
    {
        var account = JsonSerializer.Deserialize<Account>(Reserva.Replace("\"" + ReservaCategoryId + "\"", "null"), _options)!;

        account.Category.Should().BeNull();
    }

    [Fact]
    public void Read_UnknownCategoryId_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(Reserva.Replace(ReservaCategoryId, "5db5c88d-0000-0000-0000-000000000099"), _options);

        act.Should().Throw<JsonException>().WithMessage("Category '5db5c88d-0000-0000-0000-000000000099' referenced but not found*");
    }

    [Fact]
    public void Read_BankWithACategory_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(V4Bank.Replace("\"CategoryId\":null", "\"CategoryId\":\"" + ReservaCategoryId + "\""), _options);

        act.Should().Throw<ArgumentException>().WithMessage("Chase has no category setting.");
    }

    [Fact]
    public void Read_NegativeOpeningBalance_Throws()
    {
        var act = () => JsonSerializer.Deserialize<Account>(
            """{"Id":"5db5c88d-0000-0000-0000-000000000003","Name":"Chase","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":-1,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":true}}""",
            _options);

        act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
    }

    [Fact]
    public void ReadThenWrite_SettlementAccount_KeepsItsCurrenciesAndOtherParticipant()
    {
        var account = JsonSerializer.Deserialize<Account>(Mae, _options)!;

        using (new AssertionScope())
        {
            account.Template.Should().Be(AccountTemplate.Settlement);
            account.Currency.Should().Be(Currency.BRL);
            account.Currencies.Should().Equal(Currency.BRL, Currency.GBP);
            account.OtherParticipant.Should().Be("Mãe");
            JsonSerializer.Serialize(account, _options).Should().Be(Mae);
        }
    }

    [Fact]
    public void ReadThenWrite_SettlementAccountWithoutOtherParticipant_OmitsTheKey()
    {
        var json = Mae.Replace(",\"OtherParticipant\":\"Mãe\"", "");

        var account = JsonSerializer.Deserialize<Account>(json, _options)!;

        using (new AssertionScope())
        {
            account.OtherParticipant.Should().BeNull();
            JsonSerializer.Serialize(account, _options).Should().Be(json);
        }
    }

    [Fact]
    public void Write_NewCreditCardAccount_HasNoCurrenciesOrOtherParticipantKeys()
    {
        var json = JsonSerializer.Serialize(Account.Create("Amex", AccountTemplate.CreditCard, Currency.GBP), _options);

        using (new AssertionScope())
        {
            json.Should().NotContain("Currencies");
            json.Should().NotContain("OtherParticipant");
        }
    }

    [Fact]
    public void Read_BankWithCurrencies_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(V4Bank.Replace("\"CategoryId\":null", "\"CategoryId\":null,\"Currencies\":[\"GBP\"]"), _options);

        act.Should().Throw<JsonException>()
            .WithMessage("Account 5db5c88d-0000-0000-0000-000000000001 must have Currencies exactly when its template has several currencies.");
    }

    [Fact]
    public void Read_SettlementWithoutCurrencies_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(Mae.Replace("\"Currencies\":[\"BRL\",\"GBP\"],", ""), _options);

        act.Should().Throw<JsonException>()
            .WithMessage("Account 5db5c88d-0000-0000-0000-000000000008 must have Currencies exactly when its template has several currencies.");
    }

    [Fact]
    public void Read_SettlementWhoseCurrencyIsNotTheFirstOfItsCurrencies_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(Mae.Replace("\"Currency\":\"BRL\"", "\"Currency\":\"GBP\""), _options);

        act.Should().Throw<JsonException>()
            .WithMessage("Account 5db5c88d-0000-0000-0000-000000000008 Currency must be the first of its Currencies.");
    }

    [Fact]
    public void Read_BankWithAnOtherParticipant_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(V4Bank.Replace("\"CategoryId\":null", "\"CategoryId\":null,\"OtherParticipant\":\"Mãe\""), _options);

        act.Should().Throw<JsonException>()
            .WithMessage("Account 5db5c88d-0000-0000-0000-000000000001 can name another participant only when its template has participants.");
    }

    [Fact]
    public void Read_SettlementInACurrencyTheTemplateDoesNotAllow_Fails()
    {
        var act = () => JsonSerializer.Deserialize<Account>(Mae.Replace("[\"BRL\",\"GBP\"]", "[\"BRL\",\"USD\"]"), _options);

        act.Should().Throw<ArgumentException>().WithMessage("Settlement accounts must be BRL or GBP.");
    }
}
