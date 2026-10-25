using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public class CashFlowDataConverterTests
{
    private const string BarclaysId = "5db5c88d-0000-0000-0000-000000000001";
    private const string AmexId = "5db5c88d-0000-0000-0000-000000000501";
    private const string RetiredId = "5db5c88d-0000-0000-0000-000000000502";
    private const string CategoryId = "5db5c88d-0000-0000-0000-000000000201";

    private const string V2File =
        "{\"ReserveBuckets\":[],"
        + "\"Expenses\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000301\",\"Date\":\"2026-10-03\",\"Description\":\"Taxi\",\"Value\":30.5,\"CategoryId\":\"" + CategoryId + "\",\"PaymentSourceAccountId\":null,\"CreditCardId\":\"" + AmexId + "\",\"ChargeDate\":\"2026-10-03\",\"InvoiceDate\":\"2026-10-01\",\"RoundUpAmount\":null,\"CountsAsTithe\":true,\"PaymentStatus\":\"CreditCardCharge\",\"RoundUpSuggestion\":0.5,\"SuggestedRoundUpAmount\":null,\"IsInvestment\":false,\"ReportingDate\":\"2026-10-01\",\"ListingDate\":\"2026-10-01\"}],"
        + "\"ReserveMovements\":[],"
        + "\"CardStatements\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000401\",\"CreditCardId\":\"" + AmexId + "\",\"Year\":2026,\"Month\":10,\"IsPaid\":false}],"
        + "\"RecurringBills\":[],\"MaeLedgerEntries\":[],\"InvestmentSnapshots\":[],"
        + "\"InvestmentAccounts\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000601\",\"Name\":\"Amex balance\",\"IsActive\":true,\"IsLiability\":true,\"Source\":\"CreditCard\",\"CreditCardId\":\"" + AmexId + "\"}],"
        + "\"Accounts\":[{\"Id\":\"" + BarclaysId + "\",\"Name\":\"Barclays\",\"Template\":\"Bank\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":{\"Amount\":100,\"StartDate\":\"2026-09-01\"},\"RoundUp\":{\"Enabled\":false}}],"
        + "\"IncomeSources\":[],\"Incomes\":[],"
        + "\"CreditCards\":[{\"Id\":\"" + AmexId + "\",\"Name\":\"BA Amex\",\"IsActive\":true,\"NextInvoiceDueDate\":\"2026-11-18\"},{\"Id\":\"" + RetiredId + "\",\"Name\":\"Retired\",\"IsActive\":false,\"NextInvoiceDueDate\":null}],"
        + "\"Categories\":[{\"Id\":\"" + CategoryId + "\",\"Name\":\"Mercado\",\"Active\":true,\"IsInvestment\":false,\"IsTithe\":false}],"
        + "\"Transfers\":[],\"BalanceAdjustments\":[],\"TitheCarryForwards\":[],\"TitheCarryForwardEffectiveFrom\":null,\"Version\":2}";

    private readonly CashFlowSerializerAdapter _serializer = new();

    [Fact]
    public void LoadAndSave_V2FileWithCards_IsByteIdentical()
    {
        var saved = _serializer.Serialize(_serializer.Deserialize(V2File));

        saved.Should().Be(V2File);
    }

    [Fact]
    public void Load_V2Cards_BecomeCreditCardAccountsAfterTheBanksWithTheirIdsFlagsAndDueDates()
    {
        var data = _serializer.Deserialize(V2File);

        using (new AssertionScope())
        {
            data.Accounts.Select(account => (account.Id.ToString(), account.Template)).Should().Equal(
                (BarclaysId, AccountTemplate.Bank), (AmexId, AccountTemplate.CreditCard), (RetiredId, AccountTemplate.CreditCard));
            var amex = data.Accounts.Single(account => account.Id == Guid.Parse(AmexId));
            amex.Name.Should().Be("BA Amex");
            amex.Currency.Should().Be(Currency.GBP);
            amex.IsActive.Should().BeTrue();
            amex.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 11, 18));
            amex.OpeningBalance.Should().BeNull();
            amex.RoundUp.Should().BeNull();
            data.Accounts.Single(account => account.Id == Guid.Parse(RetiredId)).IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public void Load_CardReferences_ShareTheCardAccountInstance()
    {
        var data = _serializer.Deserialize(V2File);
        var amex = data.Accounts.Single(account => account.Id == Guid.Parse(AmexId));

        using (new AssertionScope())
        {
            data.Expenses.Single().CreditCard.Should().BeSameAs(amex);
            data.CardStatements.Single().CreditCard.Should().BeSameAs(amex);
            data.InvestmentAccounts.Single().CreditCard.Should().BeSameAs(amex);
        }
    }
}
