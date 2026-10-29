using System.Text.Json;
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
    private const string ReservaId = "5db5c88d-0000-0000-0000-000000000801";
    private const string PotId = "5db5c88d-0000-0000-0000-000000000802";
    private const string ReservaCategoryId = "5db5c88d-0000-0000-0000-000000000202";
    private const string ReservasPessoaisId = "5db5c88d-0000-0000-0000-000000000602";
    private const string ArianaId = "5db5c88d-0000-0000-0000-000000000901";
    private const string GleisonId = "5db5c88d-0000-0000-0000-000000000902";
    private const string ArianaPotsAccount = "\"Group\":\"Salary\",\"PotsAccountId\":\"" + ReservaId + "\"}";

    private const string V4File =
        "{\"Expenses\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000301\",\"Date\":\"2026-10-03\",\"Description\":\"Taxi\",\"Value\":30.5,\"CategoryId\":\"" + CategoryId + "\",\"PaymentSourceAccountId\":null,\"CreditCardId\":\"" + AmexId + "\",\"ChargeDate\":\"2026-10-03\",\"InvoiceDate\":\"2026-10-01\",\"RoundUpAmount\":null,\"CountsAsTithe\":true,\"PaymentStatus\":\"CreditCardCharge\",\"RoundUpSuggestion\":0.5,\"SuggestedRoundUpAmount\":null,\"IsInvestment\":false,\"ReportingDate\":\"2026-10-01\",\"ListingDate\":\"2026-10-01\"}],"
        + "\"ReserveMovements\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000901\",\"BucketId\":\"" + PotId + "\",\"Amount\":40,\"Date\":\"2026-10-05\",\"Description\":\"Deposit\",\"IncomeId\":null}],"
        + "\"CardStatements\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000401\",\"CreditCardId\":\"" + AmexId + "\",\"Year\":2026,\"Month\":10,\"IsPaid\":false,\"DueDate\":null},"
        + "{\"Id\":\"5db5c88d-0000-0000-0000-000000000402\",\"CreditCardId\":\"" + AmexId + "\",\"Year\":2026,\"Month\":11,\"IsPaid\":false,\"DueDate\":\"2026-11-18\"}],"
        + "\"RecurringBills\":[],\"MaeLedgerEntries\":[],\"InvestmentSnapshots\":[],"
        + "\"InvestmentAccounts\":[{\"Id\":\"5db5c88d-0000-0000-0000-000000000601\",\"Name\":\"Amex balance\",\"IsActive\":true,\"IsLiability\":true,\"Source\":\"CreditCard\",\"CreditCardId\":\"" + AmexId + "\",\"PotsAccountId\":null},"
        + "{\"Id\":\"" + ReservasPessoaisId + "\",\"Name\":\"Reservas pessoais\",\"IsActive\":true,\"IsLiability\":false,\"Source\":\"PotsAccountTotal\",\"CreditCardId\":null,\"PotsAccountId\":\"" + ReservaId + "\"}],"
        + "\"Accounts\":[{\"Id\":\"" + BarclaysId + "\",\"Name\":\"Barclays\",\"Template\":\"Bank\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":{\"Amount\":100,\"StartDate\":\"2026-09-01\"},\"RoundUp\":{\"Enabled\":false},\"Pots\":null,\"CategoryId\":null},"
        + "{\"Id\":\"" + AmexId + "\",\"Name\":\"BA Amex\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":null,\"RoundUp\":null,\"Pots\":null,\"CategoryId\":null},"
        + "{\"Id\":\"" + RetiredId + "\",\"Name\":\"Ita\\u00FA Retired\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":false,\"OpeningBalance\":null,\"RoundUp\":null,\"Pots\":null,\"CategoryId\":null},"
        + "{\"Id\":\"" + ReservaId + "\",\"Name\":\"Reserva\",\"Template\":\"Pots\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":null,\"RoundUp\":null,"
        + "\"Pots\":[{\"Id\":\"" + PotId + "\",\"Name\":\"Investimento\",\"IsActive\":true,\"SplitPercentage\":33.33}],\"CategoryId\":\"" + ReservaCategoryId + "\"}],"
        + "\"IncomeSources\":[{\"Id\":\"" + ArianaId + "\",\"Name\":\"Ariana\",\"IsActive\":true," + ArianaPotsAccount + ","
        + "{\"Id\":\"" + GleisonId + "\",\"Name\":\"Gleison\",\"IsActive\":true,\"Group\":\"Salary\",\"PotsAccountId\":null}],\"Incomes\":[],"
        + "\"Categories\":[{\"Id\":\"" + CategoryId + "\",\"Name\":\"Mercado\",\"Active\":true,\"IsInvestment\":false,\"IsTithe\":false},"
        + "{\"Id\":\"" + ReservaCategoryId + "\",\"Name\":\"Reserva\",\"Active\":true,\"IsInvestment\":false,\"IsTithe\":false}],"
        + "\"Transfers\":[],\"BalanceAdjustments\":[],\"TitheCarryForwards\":[],\"TitheCarryForwardEffectiveFrom\":null,\"Version\":4}";

    private const string UnpaidOctoberStatement = "\"Year\":2026,\"Month\":10,\"IsPaid\":false,\"DueDate\":null}";
    private const string PaidOctoberStatement = "\"Year\":2026,\"Month\":10,\"IsPaid\":true,\"DueDate\":null,\"PaymentTransferId\":\"" + PaymentId + "\"}";

    private static readonly string V4FileWithPayment = V4File
        .Replace(UnpaidOctoberStatement, PaidOctoberStatement)
        .Replace(
            "\"Transfers\":[]",
            "\"Transfers\":[{\"Id\":\"" + PaymentId + "\",\"Date\":\"2026-11-12\",\"SourceAccountId\":\"" + BarclaysId + "\",\"DestinationAccountId\":\"" + AmexId + "\",\"Amount\":30.5,\"Note\":null}]");

    private readonly CashFlowSerializerAdapter _serializer = new();

    [Fact]
    public void LoadAndSave_V4FileWithCardsAndPots_IsByteIdentical()
    {
        var saved = _serializer.Serialize(_serializer.Deserialize(V4File));

        saved.Should().Be(V4File);
    }

    [Fact]
    public void LoadAndSave_StatementPaidByTransfer_IsByteIdentical()
    {
        var saved = _serializer.Serialize(_serializer.Deserialize(V4FileWithPayment));

        using (new AssertionScope())
        {
            V4FileWithPayment.Should().Contain("\"PaymentTransferId\"");
            saved.Should().Be(V4FileWithPayment);
        }
    }

    [Fact]
    public void Load_StatementPaidByTransfer_LinksTheStatementToItsPayment()
    {
        var data = _serializer.Deserialize(V4FileWithPayment);

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
    public void Load_V4Cards_AreCreditCardAccountsAfterTheBanksWithTheirIdsAndFlags()
    {
        var data = _serializer.Deserialize(V4File);

        using (new AssertionScope())
        {
            data.Accounts.Select(account => (account.Id.ToString(), account.Template)).Should().Equal(
                (BarclaysId, AccountTemplate.Bank), (AmexId, AccountTemplate.CreditCard), (RetiredId, AccountTemplate.CreditCard), (ReservaId, AccountTemplate.Pots));
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
    public void Load_V4Statements_CarryTheirDueDates()
    {
        var data = _serializer.Deserialize(V4File);

        data.CardStatements.Select(statement => (statement.Month, statement.DueDate)).Should().Equal(
            (10, (DateOnly?)null), (11, new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void Load_CardReferences_ShareTheCardAccountInstance()
    {
        var data = _serializer.Deserialize(V4File);
        var amex = data.Accounts.Single(account => account.Id == Guid.Parse(AmexId));

        using (new AssertionScope())
        {
            data.Expenses.Single().CreditCard.Should().BeSameAs(amex);
            data.CardStatements.Should().OnlyContain(statement => ReferenceEquals(statement.CreditCard, amex));
            data.InvestmentAccounts.Single(account => account.Source == InvestmentAccountSource.CreditCard).CreditCard.Should().BeSameAs(amex);
        }
    }

    [Fact]
    public void Load_PotsSourcedInvestmentAccount_SharesThePotsAccountInstance()
    {
        var data = _serializer.Deserialize(V4File);
        var reserva = data.Accounts.Single(account => account.Id == Guid.Parse(ReservaId));

        var investmentAccount = data.InvestmentAccounts.Single(account => account.Id == Guid.Parse(ReservasPessoaisId));

        using (new AssertionScope())
        {
            investmentAccount.Source.Should().Be(InvestmentAccountSource.PotsAccountTotal);
            investmentAccount.PotsAccount.Should().BeSameAs(reserva);
        }
    }

    [Fact]
    public void Load_IncomeSources_ShareTheirPotsAccountInstance()
    {
        var data = _serializer.Deserialize(V4File);
        var reserva = data.Accounts.Single(account => account.Id == Guid.Parse(ReservaId));

        using (new AssertionScope())
        {
            data.IncomeSources.Single(source => source.Id == Guid.Parse(ArianaId)).PotsAccount.Should().BeSameAs(reserva);
            data.IncomeSources.Single(source => source.Id == Guid.Parse(GleisonId)).PotsAccount.Should().BeNull();
        }
    }

    [Fact]
    public void Load_IncomeSourceWithUnknownPotsAccountId_FailsTheLoad()
    {
        var file = V4File.Replace(ArianaPotsAccount, "\"Group\":\"Salary\",\"PotsAccountId\":\"5db5c88d-0000-0000-0000-000000000999\"}");

        var act = () => _serializer.Deserialize(file);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Load_UnknownPotsAccountId_FailsTheLoad()
    {
        var file = V4File.Replace("\"PotsAccountId\":\"" + ReservaId + "\"", "\"PotsAccountId\":\"5db5c88d-0000-0000-0000-000000000999\"");

        var act = () => _serializer.Deserialize(file);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Load_V4Pots_LiveInTheirAccountAndMovementsShareThePotInstance()
    {
        var data = _serializer.Deserialize(V4File);

        using (new AssertionScope())
        {
            var reserva = data.Accounts.Single(account => account.Id == Guid.Parse(ReservaId));
            var pot = reserva.Pots.Should().ContainSingle().Which;
            pot.Id.Should().Be(Guid.Parse(PotId));
            pot.Name.Should().Be("Investimento");
            pot.SplitPercentage.Should().Be(33.33m);
            data.ReserveMovements.Single().Bucket.Should().BeSameAs(pot);
            data.Accounts.Where(account => account.Id != reserva.Id).Should().OnlyContain(account => account.Pots == null);
        }
    }

    [Fact]
    public void Load_V4PotsAccountCategory_IsTheLoadedCategoryInstance()
    {
        var data = _serializer.Deserialize(V4File);

        using (new AssertionScope())
        {
            data.Accounts.Single(account => account.Id == Guid.Parse(ReservaId)).Category
                .Should().BeSameAs(data.Categories.Single(category => category.Id == Guid.Parse(ReservaCategoryId)));
            data.Accounts.Where(account => account.Id != Guid.Parse(ReservaId)).Should().OnlyContain(account => account.Category == null);
        }
    }

    [Fact]
    public void Load_PotsAccountWithAnUnknownCategory_Fails()
    {
        var act = () => _serializer.Deserialize(V4File.Replace("\"CategoryId\":\"" + ReservaCategoryId, "\"CategoryId\":\"5db5c88d-0000-0000-0000-000000000299"));

        act.Should().Throw<JsonException>().WithMessage("Category '5db5c88d-0000-0000-0000-000000000299' referenced but not found*");
    }

    [Fact]
    public void Load_MovementOfAnUnknownPot_Fails()
    {
        var act = () => _serializer.Deserialize(V4File.Replace("\"BucketId\":\"" + PotId, "\"BucketId\":\"5db5c88d-0000-0000-0000-000000000899"));

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("\"RoundUp\":null,\"Pots\":[{", "\"RoundUp\":null,\"Other\":[{")]
    [InlineData("\"Name\":\"BA Amex\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":null,\"RoundUp\":null,\"Pots\":null", "\"Name\":\"BA Amex\",\"Template\":\"CreditCard\",\"Currency\":\"GBP\",\"IsActive\":true,\"OpeningBalance\":null,\"RoundUp\":null,\"Pots\":[]")]
    public void Load_PotsPresentWithoutThePotsTemplateOrMissingWithIt_Fails(string original, string replacement)
    {
        var act = () => _serializer.Deserialize(V4File.Replace(original, replacement));

        act.Should().Throw<JsonException>().WithMessage("*must have Pots exactly when its template has pots*");
    }

    [Fact]
    public void Load_V3File_IsRefusedWithTheMigrateMessage()
    {
        var act = () => _serializer.Deserialize("""{ "Accounts": [], "ReserveBuckets": [], "Version": 3 }""");

        act.Should().Throw<InvalidDataException>()
            .WithMessage("data-cashflow.json is version 3; this build needs version 4. Run the migrate command first.");
    }
}
