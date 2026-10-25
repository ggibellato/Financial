using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

[Trait("Category", "Unit")]
public class V2ToV3CardsToAccountsTests
{
    private readonly V2ToV3CardsToAccounts _sut = new();

    private static JsonObject Root(string json = "") => JsonNode.Parse(json == "" ? V2DataFile.Json : json)!.AsObject();

    private static List<JsonObject> Statements(JsonObject root) => root["CardStatements"]!.AsArray().Select(s => s!.AsObject()).ToList();

    private static JsonObject StatementFor(JsonObject root, string cardId, int month) =>
        Statements(root).Single(s => (string)s["CreditCardId"]! == cardId && (int)s["Month"]! == month);

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-01")]
    public void Apply_FiveCards_BecomeCreditCardAccountsWithSameIdsAndFlags()
    {
        var root = Root();

        var summary = _sut.Apply(root);

        using (new AssertionScope())
        {
            root.ContainsKey("CreditCards").Should().BeFalse();
            root["Accounts"]!.AsArray().Select(a => a!.ToJsonString()).Should().Equal(
                $$$"""{"Id":"{{{V2DataFile.BarclaysId}}}","Name":"Barclays","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":100,"StartDate":"2026-01-01"},"RoundUp":{"Enabled":false}}""",
                $$$"""{"Id":"{{{V2DataFile.Visa8003Id}}}","Name":"Platinum Visa 8003","Template":"CreditCard","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null}""",
                $$$"""{"Id":"{{{V2DataFile.Visa6007Id}}}","Name":"Platinum Visa 6007","Template":"CreditCard","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null}""",
                $$$"""{"Id":"{{{V2DataFile.ChaseMasterId}}}","Name":"Chase Master 4023","Template":"CreditCard","Currency":"GBP","IsActive":false,"OpeningBalance":null,"RoundUp":null}""",
                $$$"""{"Id":"{{{V2DataFile.AmexId}}}","Name":"BA Amex","Template":"CreditCard","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null}""",
                $$$"""{"Id":"{{{V2DataFile.PaypalId}}}","Name":"Paypal Credit","Template":"CreditCard","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null}""");
            summary.Should().Be("5 cards became accounts; 3 statements added; 2 due dates moved to statements");
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-01")]
    public void Apply_CardDueDate_MovesToTheStatementOfItsMonth()
    {
        var root = Root();

        _sut.Apply(root);

        var amexOctober = StatementFor(root, V2DataFile.AmexId, 10);
        var visaNovember = StatementFor(root, V2DataFile.Visa8003Id, 11);
        using (new AssertionScope())
        {
            ((string)amexOctober["Id"]!).Should().Be(V2DataFile.AmexOctoberStatementId);
            ((string)amexOctober["DueDate"]!).Should().Be("2026-10-25");
            ((bool)visaNovember["IsPaid"]!).Should().BeFalse();
            ((string)visaNovember["DueDate"]!).Should().Be("2026-11-18");
            Guid.TryParse((string)visaNovember["Id"]!, out _).Should().BeTrue();
            Statements(root).Where(s => s != amexOctober && s != visaNovember)
                .Should().OnlyContain(s => s.ContainsKey("DueDate") && s["DueDate"] == null);
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-02")]
    public void Apply_MonthWithChargesAndNoStatement_AddsStatementPaidOnlyWhenAllSettled()
    {
        var root = Root();

        _sut.Apply(root);

        using (new AssertionScope())
        {
            ((bool)StatementFor(root, V2DataFile.Visa6007Id, 9)["IsPaid"]!).Should().BeTrue();
            ((bool)StatementFor(root, V2DataFile.Visa6007Id, 8)["IsPaid"]!).Should().BeFalse();
            ((string)StatementFor(root, V2DataFile.Visa8003Id, 10)["Id"]!).Should().Be(V2DataFile.Visa8003OctoberStatementId);
            Statements(root).Should().HaveCount(5);
        }
    }

    public static TheoryData<string, string> UnknownCardReferences => new()
    {
        { "Expense", V2DataFile.UnpaidChargeId },
        { "CardStatement", V2DataFile.Visa8003OctoberStatementId },
        { "InvestmentAccount", V2DataFile.InvestmentAccountId }
    };

    [Theory]
    [MemberData(nameof(UnknownCardReferences))]
    public void Apply_UnknownCardReference_StopsNamingTheRecordAndTheCard(string recordName, string recordId)
    {
        var unknownCardId = Guid.NewGuid().ToString();
        var json = recordName switch
        {
            "Expense" => V2DataFile.WithExpenseCard(unknownCardId),
            "CardStatement" => V2DataFile.WithStatementCard(unknownCardId),
            _ => V2DataFile.WithInvestmentAccountCard(unknownCardId)
        };
        var root = Root(json);

        var act = () => _sut.Apply(root);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>().WithMessage($"{recordName} {recordId} references unknown card {unknownCardId}");
            root.ContainsKey("CreditCards").Should().BeTrue();
        }
    }
}
