using System.Text.Json.Nodes;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

[Trait("Category", "Unit")]
public class DataFileMigratorTests
{
    private const string SourceName = "copy.json";

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-01")]
    public void Migrate_Version1File_RunsAllStepsToVersion4()
    {
        var result = DataFileMigrator.CreateDefault().Migrate(V1DataFile.Json, SourceName);

        var data = new CashFlowSerializerAdapter().Deserialize(result.Json);
        var accounts = data.Accounts.Where(a => a.Template == AccountTemplate.Bank).ToDictionary(a => a.Id.ToString());
        using (new AssertionScope())
        {
            result.FromVersion.Should().Be(1);
            result.ToVersion.Should().Be(4);
            JsonNode.Parse(result.Json)!["Version"]!.GetValue<int>().Should().Be(4);
            data.Accounts.Last().Should().BeEquivalentTo(new { Name = "Reserva", Template = AccountTemplate.Pots, Pots = Array.Empty<object>(), Category = new { Name = "Reserva" } });
            accounts.Keys.Should().BeEquivalentTo(V1DataFile.BarclaysId, V1DataFile.Trading212Id, V1DataFile.ChaseId);
            accounts.Values.Should().OnlyContain(a => a.Template == AccountTemplate.Bank && a.Currency == Currency.GBP && a.IsActive);
            accounts[V1DataFile.BarclaysId].Name.Should().Be("Barclays");
            accounts[V1DataFile.BarclaysId].OpeningBalance!.Amount.Should().Be(0m);
            accounts[V1DataFile.BarclaysId].OpeningBalance!.StartDate.Should().Be(new DateOnly(2026, 9, 1));
            accounts[V1DataFile.BarclaysId].RoundsUp.Should().BeFalse();
            accounts[V1DataFile.Trading212Id].OpeningBalance!.StartDate.Should().Be(default);
            accounts[V1DataFile.Trading212Id].RoundsUp.Should().BeTrue();
            accounts[V1DataFile.ChaseId].OpeningBalance!.Amount.Should().Be(1250.70m);
            accounts[V1DataFile.ChaseId].RoundsUp.Should().BeTrue();
            data.Expenses.Single(e => e.Id.ToString() == V1DataFile.BankExpenseId).PaymentSourceAccount.Should().BeSameAs(accounts[V1DataFile.ChaseId]);
            data.Incomes.Single().Account.Should().BeSameAs(accounts[V1DataFile.BarclaysId]);
            data.Transfers.Single().DestinationAccount.Should().BeSameAs(accounts[V1DataFile.Trading212Id]);
            data.BalanceAdjustments.Single().Account.Should().BeSameAs(accounts[V1DataFile.BarclaysId]);
            result.StepSummaries.Should().Equal(
                "3 banks became accounts; 5 references renamed",
                "0 cards became accounts; 0 statements added; 0 due dates moved to statements",
                "Reserva Pots account created with 0 pots; category Reserva; 0 snapshot sources and 0 income sources point at it");
        }
    }

    [Fact]
    public void Migrate_Version1File_WritesWhatTheSerializerWould()
    {
        var serializer = new CashFlowSerializerAdapter();

        var result = DataFileMigrator.CreateDefault().Migrate(V1DataFile.Json, SourceName);

        serializer.Serialize(serializer.Deserialize(result.Json)).Should().Be(result.Json);
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-02")]
    public void Migrate_CurrentVersionFile_ReturnsTheInputUnchanged()
    {
        var current = DataFileMigrator.CreateDefault().Migrate(V1DataFile.Json, SourceName).Json;

        var result = DataFileMigrator.CreateDefault().Migrate(current, SourceName);

        using (new AssertionScope())
        {
            result.WasAlreadyCurrent.Should().BeTrue();
            result.Json.Should().BeSameAs(current);
            result.StepSummaries.Should().BeEmpty();
        }
    }

    [Fact]
    public void Migrate_FileWithoutVersion_IsTreatedAsVersion1()
    {
        var unversioned = JsonNode.Parse(V1DataFile.Json)!.AsObject();
        unversioned.Remove("Version");

        var result = DataFileMigrator.CreateDefault().Migrate(unversioned.ToJsonString(), SourceName);

        result.FromVersion.Should().Be(1);
    }

    [Fact]
    public void Migrate_VersionAboveCurrent_Refused()
    {
        var act = () => DataFileMigrator.CreateDefault().Migrate("""{ "Version": 5 }""", SourceName);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("copy.json is version 5; this build needs version 4.");
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-01")]
    public void Migrate_Version2File_LoadsCardsAsAccountsWithEveryReferenceAndDueDate()
    {
        var result = DataFileMigrator.CreateDefault().Migrate(V2DataFile.Json, SourceName);

        var data = new CashFlowSerializerAdapter().Deserialize(result.Json);
        var cards = data.Accounts.Where(a => a.Has(Capability.HasStatements)).ToDictionary(a => a.Id.ToString());
        using (new AssertionScope())
        {
            result.FromVersion.Should().Be(2);
            result.StepSummaries.Should().Equal(
                "5 cards became accounts; 3 statements added; 2 due dates moved to statements",
                "Reserva Pots account created with 0 pots; category Reserva; 0 snapshot sources and 0 income sources point at it");
            cards.Keys.Should().Equal(V2DataFile.Visa8003Id, V2DataFile.Visa6007Id, V2DataFile.ChaseMasterId, V2DataFile.AmexId, V2DataFile.PaypalId);
            cards.Values.Should().OnlyContain(a => a.Template == AccountTemplate.CreditCard && a.Currency == Currency.GBP);
            cards[V2DataFile.ChaseMasterId].IsActive.Should().BeFalse();
            data.Expenses.Single(e => e.Id.ToString() == V2DataFile.UnpaidChargeId).CreditCard.Should().BeSameAs(cards[V2DataFile.Visa8003Id]);
            data.InvestmentAccounts.Single().CreditCard.Should().BeSameAs(cards[V2DataFile.Visa8003Id]);
            data.CardStatements.Should().OnlyContain(s => ReferenceEquals(s.CreditCard, cards[s.CreditCard.Id.ToString()]));
            CardDueDate.For(cards[V2DataFile.Visa8003Id], data.CardStatements).Should().Be(new DateOnly(2026, 11, 18));
            CardDueDate.For(cards[V2DataFile.AmexId], data.CardStatements).Should().Be(new DateOnly(2026, 10, 25));
            CardDueDate.For(cards[V2DataFile.Visa6007Id], data.CardStatements).Should().BeNull();
        }
    }

    [Fact]
    public void Migrate_StepFails_NamesTheStep()
    {
        var unknownBankId = Guid.NewGuid().ToString();

        var act = () => DataFileMigrator.CreateDefault().Migrate(V1DataFile.WithBankReference(unknownBankId), SourceName);

        act.Should().Throw<InvalidDataException>()
            .WithMessage($"Migration step V1ToV2BanksToAccounts failed: Expense {V1DataFile.BankExpenseId} references unknown bank {unknownBankId}");
    }

    [Fact]
    public void Migrate_ResultThatDoesNotLoad_Refused()
    {
        var migrator = new DataFileMigrator([new RecordingStep(1)], 2, _ => throw new InvalidDataException("bad"));

        var act = () => migrator.Migrate("""{ "Version": 1 }""", SourceName);

        act.Should().Throw<InvalidDataException>().WithMessage("The migrated file does not load: bad");
    }

    [Fact]
    public void Migrate_RunsStepsInVersionOrder()
    {
        var applied = new List<int>();
        var migrator = new DataFileMigrator(
            [new RecordingStep(3, applied), new RecordingStep(1, applied), new RecordingStep(2, applied)],
            4,
            json => json);

        var result = migrator.Migrate("""{ "Version": 1 }""", SourceName);

        using (new AssertionScope())
        {
            applied.Should().Equal(1, 2, 3);
            JsonNode.Parse(result.Json)!["Version"]!.GetValue<int>().Should().Be(4);
        }
    }

    [Fact]
    public void Migrate_MissingStepInTheChain_RefusesToSkipIt()
    {
        var applied = new List<int>();
        var migrator = new DataFileMigrator([new RecordingStep(1, applied), new RecordingStep(3, applied)], 4, json => json);

        var act = () => migrator.Migrate("""{ "Version": 1 }""", SourceName);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>().WithMessage("No migration step from version 2 to 3.");
            applied.Should().Equal(1);
        }
    }

    [Fact]
    public void Migrate_VersionThatIsNotAWholeNumber_Refused()
    {
        var act = () => DataFileMigrator.CreateDefault().Migrate("""{ "Version": "two" }""", SourceName);

        act.Should().Throw<InvalidDataException>().WithMessage("""Version must be a whole number, but is "two".""");
    }

    [Fact]
    public void Migrate_InvalidJson_Refused()
    {
        var act = () => DataFileMigrator.CreateDefault().Migrate("{", SourceName);

        act.Should().Throw<InvalidDataException>().WithMessage("copy.json is not valid JSON*");
    }

    private sealed class RecordingStep(int fromVersion, List<int>? applied = null) : IDataFileMigrationStep
    {
        public int FromVersion => fromVersion;

        public string Name => $"Step{fromVersion}";

        public string Apply(JsonObject root)
        {
            applied?.Add(fromVersion);
            return Name;
        }
    }
}
