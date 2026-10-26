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
    private const string PaymentId = "5db5c88d-0000-0000-0000-000000000701";

    private const string V3File =
        "{\"ReserveBuckets\":[],"
        + "\"Expenses\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000301\",\"Date\":\"2026-10-03\",\"Description\":\"Taxi\",\"Value\":30.5,\"CategoryId\":\"" + CategoryId + "\",\"PaymentSourceAccountId\":null,\"CreditCardId\":\"" + AmexId + "\",\"ChargeDate\":\"2026-10-03\",\"InvoiceDate\":\"2026-10-01\",\"RoundUpAmount\":null,\"CountsAsTithe\":true,\"PaymentStatus\":\"CreditCardCharge\",\"RoundUpSuggestion\":0.5,\"SuggestedRoundUpAmount\":null,\"IsInvestment\":false,\"ReportingDate\":\"2026-10-01\",\"ListingDate\":\"2026-10-01\"}],"
        + "\"ReserveMovements\":[],"
        + "\"CardStatements\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000401\",\"CreditCardId\":\"" + AmexId + "\",\"Year\":2026,\"Month\":10,\"IsPaid\":false,\"DueDate\":null},"
        + "{\"Id\":\"5db5c88d-0000-0000-0000-000000000402\",\"CreditCardId\":\"" + AmexId + "\",\"Year\":2026,\"Month\":11,\"IsPaid\":false,\"DueDate\":\"2026-11-18\"}],"
        + "\"RecurringBills\":[],\"MaeLedgerEntries\":[],\"InvestmentSnapshots\":[],"
        + "\"InvestmentAccounts\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000601\",\"Name\":\"Amex balance\",\"IsActive\":true,\"IsLiability\":true,\"Source\":\"CreditCard\",\"CreditCardId\":\"" + AmexId + "\"}],"
        + "\"Accounts\":[{\"Id\":\"" + BarclaysId + "\",\"Name\":\"Barclays\",\"Template\":\"Bank\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":{\"Amount\":100,\"StartDate\":\"2026-09-01\"},\"RoundUp\":{\"Enabled\":false}},"
        + "{\"Id\":\"" + AmexId + "\",\"Name\":\"BA Amex\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":null,\"RoundUp\":null},"
        + "{\"Id\":\"" + RetiredId + "\",\"Name\":\"Ita\\u00FA Retired\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":false,\"OpeningBalance\":null,\"RoundUp\":null}],"
        + "\"IncomeSources\":[],\"Incomes\":[],"
        + "\"Categories\":[{\"Id\":\"" + CategoryId + "\",\"Name\":\"Mercado\",\"Active\":true,\"IsInvestment\":false,\"IsTithe\":false}],"
        + "\"Transfers\":[],\"BalanceAdjustments\":[],\"TitheCarryForwards\":[],\"TitheCarryForwardEffectiveFrom\":null,\"Version\":3}";

    private const string UnpaidOctoberStatement = "\"Year\":2026,\"Month\":10,\"IsPaid\":false,\"DueDate\":null}";
    private const string PaidOctoberStatement = "\"Year\":2026,\"Month\":10,\"IsPaid\":true,\"DueDate\":null,\"PaymentTransferId\":\"" + PaymentId + "\"}";

    private static readonly string V3FileWithPayment = V3File
        .Replace(UnpaidOctoberStatement, PaidOctoberStatement)
        .Replace(
            "\"Transfers\":[]",
            "\"Transfers\":[{\"Id\":\"" + PaymentId + "\",\"Date\":\"2026-11-12\",\"SourceAccountId\":\"" + BarclaysId + "\",\"DestinationAccountId\":\"" + AmexId + "\",\"Amount\":30.5,\"Note\":null}]");

    private readonly CashFlowSerializerAdapter _serializer = new();

    [Fact]
    public void LoadAndSave_V3FileWithCards_IsByteIdentical()
    {
        var saved = _serializer.Serialize(_serializer.Deserialize(V3File));

        saved.Should().Be(V3File);
    }

    [Fact]
    public void LoadAndSave_StatementPaidByTransfer_IsByteIdentical()
    {
        var saved = _serializer.Serialize(_serializer.Deserialize(V3FileWithPayment));

        using (new AssertionScope())
        {
            V3FileWithPayment.Should().Contain("\"PaymentTransferId\"");
            saved.Should().Be(V3FileWithPayment);
        }
    }

    [Fact]
    public void Load_StatementPaidByTransfer_LinksTheStatementToItsPayment()
    {
        var data = _serializer.Deserialize(V3FileWithPayment);

        using (new AssertionScope())
        {
            var statement = data.CardStatements.Single(s => s.Month == 10);
            statement.IsPaid.Should().BeTrue();
            statement.PaymentTransferId.Should().Be(Guid.Parse(PaymentId));
            var payment = data.Transfers.Single();
            payment.IsStatementPayment.Should().BeTrue();
            payment.Amount.Should().Be(30.5m);
            data.CardStatements.Single(s => s.Month == 11).PaymentTransferId.Should().BeNull();
        }
    }

    [Fact]
    public void Load_V3Cards_AreCreditCardAccountsAfterTheBanksWithTheirIdsAndFlags()
    {
        var data = _serializer.Deserialize(V3File);

        using (new AssertionScope())
        {
            data.Accounts.Select(account => (account.Id.ToString(), account.Template)).Should().Equal(
                (BarclaysId, AccountTemplate.Bank), (AmexId, AccountTemplate.CreditCard), (RetiredId, AccountTemplate.CreditCard));
            var amex = data.Accounts.Single(account => account.Id == Guid.Parse(AmexId));
            amex.Name.Should().Be("BA Amex");
            amex.Currency.Should().Be(Currency.GBP);
            amex.IsActive.Should().BeTrue();
            amex.OpeningBalance.Should().BeNull();
            amex.RoundUp.Should().BeNull();
            var retired = data.Accounts.Single(account => account.Id == Guid.Parse(RetiredId));
            retired.Name.Should().Be("Itaú Retired");
            retired.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public void Load_V3Statements_CarryTheirDueDates()
    {
        var data = _serializer.Deserialize(V3File);

        data.CardStatements.Select(statement => (statement.Month, statement.DueDate)).Should().Equal(
            (10, (DateOnly?)null), (11, new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void Load_CardReferences_ShareTheCardAccountInstance()
    {
        var data = _serializer.Deserialize(V3File);
        var amex = data.Accounts.Single(account => account.Id == Guid.Parse(AmexId));

        using (new AssertionScope())
        {
            data.Expenses.Single().CreditCard.Should().BeSameAs(amex);
            data.CardStatements.Should().OnlyContain(statement => ReferenceEquals(statement.CreditCard, amex));
            data.InvestmentAccounts.Single().CreditCard.Should().BeSameAs(amex);
        }
    }

    [Fact]
    public void Load_V2File_IsRefusedWithTheMigrateMessage()
    {
        var act = () => _serializer.Deserialize("""{ "Accounts": [], "CreditCards": [], "Version": 2 }""");

        act.Should().Throw<InvalidDataException>()
            .WithMessage("data-cashflow.json is version 2; this build needs version 3. Run the migrate command first.");
    }
}
