using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

[Trait("Category", "Unit")]
public class V1ToV2BanksToAccountsTests
{
    private readonly V1ToV2BanksToAccounts _sut = new();

    [Fact]
    public void Apply_V1Banks_BecomeBankAccountsWithTheSameValues()
    {
        var root = JsonNode.Parse(V1DataFile.Json)!.AsObject();

        _sut.Apply(root);

        var accounts = root["Accounts"]!.AsArray();
        using (new AssertionScope())
        {
            root.ContainsKey("Banks").Should().BeFalse();
            accounts.Select(a => a!.ToJsonString()).Should().Equal(
                $$$"""{"Id":"{{{V1DataFile.BarclaysId}}}","Name":"Barclays","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":0,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":false}}""",
                $$$"""{"Id":"{{{V1DataFile.Trading212Id}}}","Name":"Trading212","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":0,"StartDate":"0001-01-01"},"RoundUp":{"Enabled":true}}""",
                $$$"""{"Id":"{{{V1DataFile.ChaseId}}}","Name":"Chase","Template":"Bank","Currency":"GBP","IsActive":true,"OpeningBalance":{"Amount":1250.70,"StartDate":"2026-09-01"},"RoundUp":{"Enabled":true}}""");
        }
    }

    [Fact]
    public void Apply_ReferenceKeys_AreRenamedKeepingTheirValues()
    {
        var root = JsonNode.Parse(V1DataFile.Json)!.AsObject();

        var summary = _sut.Apply(root);

        var cardExpense = root["Expenses"]![0]!.AsObject();
        var bankExpense = root["Expenses"]![1]!.AsObject();
        var income = root["Incomes"]![0]!.AsObject();
        var transfer = root["Transfers"]![0]!.AsObject();
        var adjustment = root["BalanceAdjustments"]![0]!.AsObject();
        using (new AssertionScope())
        {
            cardExpense.ContainsKey("PaymentSourceBankId").Should().BeFalse();
            cardExpense.ContainsKey("PaymentSourceAccountId").Should().BeTrue();
            cardExpense["PaymentSourceAccountId"].Should().BeNull();
            ((string?)bankExpense["PaymentSourceAccountId"]).Should().Be(V1DataFile.ChaseId);
            ((string?)income["AccountId"]).Should().Be(V1DataFile.BarclaysId);
            income.ContainsKey("BankId").Should().BeFalse();
            ((string?)transfer["SourceAccountId"]).Should().Be(V1DataFile.BarclaysId);
            ((string?)transfer["DestinationAccountId"]).Should().Be(V1DataFile.Trading212Id);
            ((string?)adjustment["AccountId"]).Should().Be(V1DataFile.BarclaysId);
            summary.Should().Be("3 banks became accounts; 5 references renamed");
        }
    }

    [Fact]
    public void Apply_ReferenceToUnknownBank_ThrowsNamingTheRecord()
    {
        var unknownBankId = Guid.NewGuid().ToString();
        var root = JsonNode.Parse(V1DataFile.WithBankReference(unknownBankId))!.AsObject();

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>()
            .WithMessage($"Expense {V1DataFile.BankExpenseId} references unknown bank {unknownBankId}");
    }

    [Theory]
    [InlineData("""{ "Banks": [null] }""", "Banks contains a value that is not an object: null")]
    [InlineData("""{ "Banks": [{ "Id": 7, "Name": "Barclays" }] }""", "Bank has an Id that is not a string: 7")]
    [InlineData("""{ "Banks": [], "Incomes": [{ "Id": "i1", "BankId": 7 }] }""", "Income i1 BankId has an Id that is not a string: 7")]
    public void Apply_MalformedBankOrReference_ThrowsInvalidDataException(string json, string message)
    {
        var root = JsonNode.Parse(json)!.AsObject();

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>().WithMessage(message);
    }

    [Fact]
    public void Apply_FileWithoutBanks_WritesAnEmptyAccountsCollection()
    {
        var root = JsonNode.Parse("""{ "Expenses": [] }""")!.AsObject();

        var summary = _sut.Apply(root);

        using (new AssertionScope())
        {
            root["Accounts"]!.AsArray().Should().BeEmpty();
            summary.Should().Be("0 banks became accounts; 0 references renamed");
        }
    }
}
