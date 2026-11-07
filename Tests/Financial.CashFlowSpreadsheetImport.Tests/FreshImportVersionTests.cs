using System.Text.Json.Nodes;
using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;
using Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests;

[Trait("Category", "Integration")]
public sealed class FreshImportVersionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "cf-import-" + Guid.NewGuid().ToString("N"));
    private readonly string _workbookPath;
    private readonly string _outputPath;

    public FreshImportVersionTests()
    {
        Directory.CreateDirectory(_directory);
        _workbookPath = Path.Combine(_directory, "workbook.xlsx");
        _outputPath = Path.Combine(_directory, "data-cashflow.json");

        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Notes");
        var controleMae = workbook.AddWorksheet("Controle mae");
        controleMae.Cell(1, 1).Value = "Deposito 28/05/2019";
        controleMae.Cell(1, 2).Value = 300.0;
        controleMae.Cell(1, 3).Value = 60.0;
        controleMae.Cell(2, 1).Value = "Mae pagou 10/02/2024";
        controleMae.Cell(2, 3).Value = -15.0;
        workbook.SaveAs(_workbookPath);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-04")]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-13")]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-07")]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-08")]
    public void Import_FreshWorkbook_WritesVersion5WithMaeSettlementAccountAndItsEntries()
    {
        var exitCode = RunImport(_workbookPath, "--output", _outputPath);

        var json = File.ReadAllText(_outputPath);
        var data = new CashFlowSerializerAdapter().Deserialize(json);
        using (new AssertionScope())
        {
            exitCode.Should().Be(0);
            var root = JsonNode.Parse(json)!.AsObject();
            root["Version"]!.GetValue<int>().Should().Be(5);
            root.ContainsKey("MaeLedgerEntries").Should().BeFalse();
            root.ContainsKey("CreditCards").Should().BeFalse();
            root.ContainsKey("ReserveBuckets").Should().BeFalse();
            var reserva = data.Accounts.Should().ContainSingle(a => a.Template == AccountTemplate.Pots).Which;
            reserva.Should().BeEquivalentTo(new { Name = "Reserva", Currency = Currency.GBP, IsActive = true, OpeningBalance = (object?)null, RoundUp = (object?)null });
            reserva.Pots!.Select(pot => pot.Name).Should().Equal("Investimento", "HouseTreats", "Ariana", "Gleison", "Samuel");
            reserva.Category.Should().BeSameAs(data.Categories.Single(c => c.Name == "Reserva"));
            data.IncomeSources.Single(s => s.Name == "Ariana").PotsAccount.Should().BeSameAs(reserva);
            data.IncomeSources.Where(s => s.Name != "Ariana").Should().OnlyContain(s => s.PotsAccount == null);
            var banks = data.Accounts.Where(a => a.Template == AccountTemplate.Bank).ToList();
            banks.Select(a => (a.Name, a.RoundsUp)).Should().Equal(("Barclays", false), ("Trading212", true), ("Chase", true));
            banks.Should().OnlyContain(a => a.Currency == Currency.GBP && a.IsActive && a.OpeningBalance!.Amount == 0m);
            data.Accounts.Where(a => a.Template == AccountTemplate.CreditCard).Should().HaveCount(5)
                .And.OnlyContain(a => a.Currency == Currency.GBP && a.IsActive && a.OpeningBalance == null && a.RoundUp == null);
            var mae = data.Accounts.Should().ContainSingle(a => a.Template == AccountTemplate.Settlement).Which;
            mae.Should().BeEquivalentTo(new { Name = "Mãe", Currency = Currency.BRL, IsActive = true, OtherParticipant = "Mãe" });
            mae.Currencies.Should().Equal(Currency.BRL, Currency.GBP);
            data.SettlementTransactions.Select(e => (e.Account, e.PaidBy, e.Currency, e.Amount, e.ConvertedValue)).Should().Equal(
                (mae, SettlementPayer.You, Currency.BRL, 300m, (decimal?)60m),
                (mae, SettlementPayer.Other, Currency.GBP, 15m, (decimal?)null));
        }
    }

    [Fact]
    public void Import_ExistingVersion1Output_RefusesWithoutWritingOrBackingUp()
    {
        File.WriteAllText(_outputPath, V1DataFile.Json);

        var exitCode = RunImport(_workbookPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            File.ReadAllText(_outputPath).Should().Be(V1DataFile.Json);
            Directory.GetFiles(_directory, "*.backup-migration-*").Should().BeEmpty();
        }
    }

    [Fact]
    public void RefusalFor_Version1Output_AsksForTheMigrateCommand()
    {
        File.WriteAllText(_outputPath, V1DataFile.Json);

        ExistingOutputVersionGuard.RefusalFor(_outputPath).Should().Be(
            $"{_outputPath} is version 1; this build needs version 5. Run the migrate command first.");
    }

    [Fact]
    public void RefusalFor_Version6Output_RefusesWithoutTheMigrateHint()
    {
        File.WriteAllText(_outputPath, """{ "Version": 6 }""");

        ExistingOutputVersionGuard.RefusalFor(_outputPath).Should().Be(
            $"{_outputPath} is version 6; this build needs version 5.");
    }

    [Fact]
    public void RefusalFor_Version5OrMissingOutput_AllowsTheImport()
    {
        var missing = ExistingOutputVersionGuard.RefusalFor(_outputPath);
        File.WriteAllText(_outputPath, """{ "Version": 5 }""");

        using (new AssertionScope())
        {
            missing.Should().BeNull();
            ExistingOutputVersionGuard.RefusalFor(_outputPath).Should().BeNull();
        }
    }

    private static int RunImport(params string[] args) =>
        (int)typeof(ImportArguments).Assembly.EntryPoint!.Invoke(null, [args])!;
}
