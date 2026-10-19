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
        workbook.SaveAs(_workbookPath);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-04")]
    public void Import_FreshWorkbook_WritesVersion2WithBankAccounts()
    {
        var exitCode = RunImport(_workbookPath, "--output", _outputPath);

        var json = File.ReadAllText(_outputPath);
        var data = new CashFlowSerializerAdapter().Deserialize(json);
        using (new AssertionScope())
        {
            exitCode.Should().Be(0);
            JsonNode.Parse(json)!["Version"]!.GetValue<int>().Should().Be(2);
            data.Accounts.Select(a => (a.Name, a.RoundsUp)).Should().Equal(("Barclays", false), ("Trading212", true), ("Chase", true));
            data.Accounts.Should().OnlyContain(a => a.Template == AccountTemplate.Bank && a.Currency == Currency.GBP && a.IsActive);
            data.Accounts.Should().OnlyContain(a => a.OpeningBalance!.Amount == 0m);
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
            $"{_outputPath} is version 1; this build needs version 2. Run the migrate command first.");
    }

    [Fact]
    public void RefusalFor_Version3Output_RefusesWithoutTheMigrateHint()
    {
        File.WriteAllText(_outputPath, """{ "Version": 3 }""");

        ExistingOutputVersionGuard.RefusalFor(_outputPath).Should().Be(
            $"{_outputPath} is version 3; this build needs version 2.");
    }

    [Fact]
    public void RefusalFor_Version2OrMissingOutput_AllowsTheImport()
    {
        var missing = ExistingOutputVersionGuard.RefusalFor(_outputPath);
        File.WriteAllText(_outputPath, """{ "Version": 2 }""");

        using (new AssertionScope())
        {
            missing.Should().BeNull();
            ExistingOutputVersionGuard.RefusalFor(_outputPath).Should().BeNull();
        }
    }

    private static int RunImport(params string[] args) =>
        (int)typeof(ImportArguments).Assembly.EntryPoint!.Invoke(null, [args])!;
}
