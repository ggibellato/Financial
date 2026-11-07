using System.Globalization;
using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

[Trait("Category", "Unit")]
public class V4ToV5SettlementAccountTests
{
    private readonly V4ToV5SettlementAccount _sut = new();

    private static JsonObject Root(string json = V4DataFile.Json) => JsonNode.Parse(json)!.AsObject();

    private static List<JsonObject> Accounts(JsonObject root) => root["Accounts"]!.AsArray().Select(a => a!.AsObject()).ToList();

    private static Dictionary<string, JsonObject> Entries(JsonObject root) =>
        root["SettlementTransactions"]!.AsArray().Select(e => e!.AsObject()).ToDictionary(e => (string)e["Id"]!);

    private static string WithEntries(string entries) => V4DataFile.Json.Replace(V4DataFile.Entries, entries);

    [Fact]
    public void FromVersion_Is4()
    {
        _sut.FromVersion.Should().Be(4);
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-01")]
    public void Apply_MaeAccount_IsSettlementWithBrlGbpAndOtherParticipantMae()
    {
        var root = Root();
        var barclaysBefore = Accounts(root).Single().ToJsonString();

        _sut.Apply(root);

        var accounts = Accounts(root);
        var mae = accounts.Last();
        using (new AssertionScope())
        {
            accounts.Should().HaveCount(2);
            accounts[0].ToJsonString().Should().Be(barclaysBefore);
            Guid.TryParse((string)mae["Id"]!, out _).Should().BeTrue();
            mae.ToJsonString().Should().Be(
                $$$"""{"Id":"{{{mae["Id"]}}}","Name":"Mãe","Template":"Settlement","Currency":"BRL","IsActive":true,"OpeningBalance":null,"RoundUp":null,"Pots":null,"CategoryId":null,"Currencies":["BRL","GBP"],"OtherParticipant":"Mãe"}"""
                    .Replace("Mãe", "M\\u00E3e"));
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-01")]
    public void Apply_Entries_MoveIntoMaeWithPaidByFromTheSignAndPositiveAmounts()
    {
        var root = Root();

        _sut.Apply(root);

        var maeId = (string)Accounts(root).Last()["Id"]!;
        var entries = root["SettlementTransactions"]!.AsArray().Select(e => e!.ToJsonString()).ToList();
        using (new AssertionScope())
        {
            root.ContainsKey("MaeLedgerEntries").Should().BeFalse();
            entries.Should().Equal(
                $$$"""{"Id":"{{{V4DataFile.PaidByYouId}}}","AccountId":"{{{maeId}}}","Date":"2026-07-01","Description":"School supplies","Note":"Term start","PaidBy":"You","Currency":"BRL","Amount":350,"ConvertedValue":51.23}""",
                $$$"""{"Id":"{{{V4DataFile.PaidByMaeId}}}","AccountId":"{{{maeId}}}","Date":"2026-07-02","Description":"Insurance","Note":"","PaidBy":"Other","Currency":"BRL","Amount":186.1234,"ConvertedValue":null}""",
                $$$"""{"Id":"{{{V4DataFile.GbpEntryId}}}","AccountId":"{{{maeId}}}","Date":"2026-07-03","Description":"Doctor","Note":"","PaidBy":"Other","Currency":"GBP","Amount":40.12345678,"ConvertedValue":300.5}""",
                $$$"""{"Id":"{{{V4DataFile.PoundEntryId}}}","AccountId":"{{{maeId}}}","Date":"2022-11-25","Description":"Presente Samuel 100 libras","Note":"libras","PaidBy":"You","Currency":"GBP","Amount":100,"ConvertedValue":654.24}""",
                $$$"""{"Id":"{{{V4DataFile.WholePoundsBrlEntryId}}}","AccountId":"{{{maeId}}}","Date":"2023-01-10","Description":"Transfer","Note":"","PaidBy":"You","Currency":"BRL","Amount":600,"ConvertedValue":100}""");
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    public void Apply_PoundEntriesTaggedBrl_AreRecordedAsPaidInGbp()
    {
        var pounds = new[] { ("2022-05-18", 80, 491.04), ("2022-11-25", 100, 654.24), ("2023-11-23", 100, 630.1), ("2024-01-03", 10, 63.5), ("2024-10-11", 80, 560) };
        var entries = "[" + string.Join(",", pounds.Select((p, i) => string.Create(CultureInfo.InvariantCulture,
            $$"""{ "Id": "7b0f0000-0000-0000-0000-00000000020{{i}}", "Date": "{{p.Item1}}", "Description": "Libras", "SourceCurrency": "BRL", "BrlValue": {{p.Item3}}, "GbpValue": {{p.Item2}} }"""))) + "]";
        var root = Root(WithEntries(entries));

        var summary = _sut.Apply(root);

        using (new AssertionScope())
        {
            Entries(root).Values.Should().OnlyContain(e => (string)e["Currency"]! == "GBP");
            Entries(root).Values.Select(e => (decimal)e["Amount"]!).Should().Equal(80m, 100m, 100m, 10m, 80m);
            summary.Should().Contain("5 pound entries recorded in GBP");
        }
    }

    [Fact]
    public void Apply_BrlEntryWithWholePoundsNotInTheList_StaysBrl()
    {
        var root = Root();

        _sut.Apply(root);

        Entries(root)[V4DataFile.WholePoundsBrlEntryId]["Currency"]!.GetValue<string>().Should().Be("BRL");
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-03")]
    public void Apply_EntryWithoutConvertedValue_KeepsItEmpty()
    {
        var root = Root();

        _sut.Apply(root);

        var entry = Entries(root)[V4DataFile.PaidByMaeId];
        entry.ContainsKey("ConvertedValue").Should().BeTrue();
        entry["ConvertedValue"].Should().BeNull();
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    public void Apply_Totals_AreUnchangedAtStoredPrecision()
    {
        var root = Root();

        var summary = _sut.Apply(root);

        var signed = Entries(root).Values.Select(e => (
            Sign: (string)e["PaidBy"]! == "You" ? 1m : -1m,
            Currency: (string)e["Currency"]!,
            Amount: (decimal)e["Amount"]!,
            Converted: e["ConvertedValue"] is null ? 0m : (decimal)e["ConvertedValue"]!)).ToList();
        using (new AssertionScope())
        {
            signed.Sum(e => e.Sign * (e.Currency == "BRL" ? e.Amount : e.Converted)).Should().Be(V4DataFile.TotalBrl);
            signed.Sum(e => e.Sign * (e.Currency == "GBP" ? e.Amount : e.Converted)).Should().Be(V4DataFile.TotalGbp);
            summary.Should().Be(
                "Mãe Settlement account created with 5 entries (3 paid by you, 2 by Mãe); 1 pound entries recorded in GBP; 1 without a converted value; "
                + "totals BRL 1117.6166 and GBP 211.10654322 unchanged");
        }
    }

    [Theory]
    [InlineData("""[]""")]
    [InlineData(null)]
    public void Apply_NoEntries_StillCreatesMae(string? entries)
    {
        var root = Root(entries is null ? V4DataFile.Json.Replace($"\"MaeLedgerEntries\": {V4DataFile.Entries},", string.Empty) : WithEntries(entries));

        var summary = _sut.Apply(root);

        using (new AssertionScope())
        {
            Accounts(root).Last()["Name"]!.GetValue<string>().Should().Be("Mãe");
            root["SettlementTransactions"]!.AsArray().Should().BeEmpty();
            summary.Should().Be("Mãe Settlement account created with 0 entries (0 paid by you, 0 by Mãe); 0 pound entries recorded in GBP; 0 without a converted value; totals BRL 0 and GBP 0 unchanged");
        }
    }

    [Fact]
    public void Apply_FileWithASettlementAccount_RefusedAndUnchanged()
    {
        var json = V4DataFile.Json.Replace("\"Name\": \"Barclays\", \"Template\": \"Bank\"", "\"Name\": \"Samuel\", \"Template\": \"Settlement\"");
        var root = Root(json);
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("'Samuel' is a Settlement account; Settlement accounts start in V5. Remove it and migrate again.");
        root.ToJsonString().Should().Be(before);
    }

    [Theory]
    [InlineData("Mãe")]
    [InlineData("MÃE")]
    public void Apply_AccountNamedMae_Refused(string name)
    {
        var root = Root(V4DataFile.Json.Replace("\"Name\": \"Barclays\"", $"\"Name\": \"{name}\""));

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>().WithMessage("An account named \"Mãe\" already exists.");
    }

    [Fact]
    public void Apply_EntryInAnotherCurrency_Refused()
    {
        var root = Root(WithEntries($$"""[{ "Id": "{{V4DataFile.PaidByYouId}}", "Date": "2026-07-01", "Description": "X", "SourceCurrency": "USD", "BrlValue": 1, "GbpValue": null }]"""));

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>()
            .WithMessage($"MaeLedgerEntry {V4DataFile.PaidByYouId} has currency USD; only BRL and GBP are allowed.");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("0")]
    public void Apply_EntryWithoutAPaidValue_RefusedAndUnchanged(string brlValue)
    {
        var root = Root(WithEntries($$"""[{ "Id": "{{V4DataFile.PaidByYouId}}", "Date": "2026-07-01", "Description": "X", "SourceCurrency": "BRL", "BrlValue": {{brlValue}}, "GbpValue": 5 }]"""));
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>().WithMessage($"MaeLedgerEntry {V4DataFile.PaidByYouId} has no BRL value.");
        root.ToJsonString().Should().Be(before);
    }

    [Theory]
    [InlineData("10", "-2")]
    [InlineData("-10", "2")]
    [InlineData("10", "0")]
    public void Apply_EntryWithDifferentSigns_Refused(string brlValue, string gbpValue)
    {
        var root = Root(WithEntries($$"""[{ "Id": "{{V4DataFile.PaidByYouId}}", "Date": "2026-07-01", "Description": "X", "SourceCurrency": "BRL", "BrlValue": {{brlValue}}, "GbpValue": {{gbpValue}} }]"""));

        var act = () => _sut.Apply(root);

        act.Should().Throw<InvalidDataException>().WithMessage($"MaeLedgerEntry {V4DataFile.PaidByYouId} has BRL and GBP values with different signs.");
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    public void EnsureTotalsUnchanged_DifferentTotal_RefusesAndWritesNothing()
    {
        var legacy = new[]
        {
            JsonNode.Parse("""{ "BrlValue": 167.8768, "GbpValue": 7.94214099 }""")!.AsObject()
        };
        var migrated = new[]
        {
            new V4ToV5SettlementAccount.MigratedEntry(
                JsonNode.Parse("""{ "PaidBy": "You", "Currency": "BRL", "Amount": 167.88, "ConvertedValue": 7.94214099 }""")!.AsObject(), IsPoundEntry: false)
        };

        var act = () => V4ToV5SettlementAccount.EnsureTotalsUnchanged(legacy, migrated);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Mãe totals would change: BRL 167.8768 → 167.88, GBP 7.94214099 → 7.94214099.");
    }
}
