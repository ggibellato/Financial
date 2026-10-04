using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.CommandLine;

public class ImportArgumentsTests
{
    private const string RefusalLine =
        "Refusing to run: --output is required and must not be the live data file (data/data-cashflow.json).";

    private const string UsageLine =
        "Usage: CashFlowSpreadsheetImport <workbook.xlsx> --output <path> [--mensais-only]";

    [Theory]
    [InlineData]
    [InlineData("a.xlsx")]
    [InlineData("--output", "out.json")]
    [InlineData("a.xlsx", "--output")]
    [InlineData("--output", "a.xlsx", "--mensais-only")]
    public void TryParse_WithoutWorkbookOrOutputValue_RefusesWithOutputRequiredMessage(params string[] args)
    {
        var parsed = ImportArguments.TryParse(args, out var arguments, out var refusal);

        parsed.Should().BeFalse();
        arguments.Should().BeNull();
        refusal.Should().Be(RefusalLine + Environment.NewLine + UsageLine);
    }

    [Fact]
    public void TryParse_SecondPositionalArgument_RefusesAndPointsToOutputFlag()
    {
        var parsed = ImportArguments.TryParse(["a.xlsx", "b.json"], out _, out var refusal);

        parsed.Should().BeFalse();
        refusal.Should().Be("Unexpected argument 'b.json'. Pass the output path with --output." + Environment.NewLine + UsageLine);
    }

    [Fact]
    public void TryParse_UnknownOption_Refuses()
    {
        var parsed = ImportArguments.TryParse(["a.xlsx", "--output", "out.json", "--force"], out _, out var refusal);

        parsed.Should().BeFalse();
        refusal.Should().Be("Unknown option '--force'." + Environment.NewLine + UsageLine);
    }

    [Fact]
    public void TryParse_ValidArguments_ReturnsWorkbookOutputAndFullRebuildMode()
    {
        var parsed = ImportArguments.TryParse(["a.xlsx", "--output", "out.json"], out var arguments, out var refusal);

        parsed.Should().BeTrue();
        refusal.Should().BeNull();
        arguments.Should().Be(new ImportArguments("a.xlsx", "out.json", MensaisOnly: false));
    }

    [Theory]
    [InlineData("--mensais-only", "a.xlsx", "--output", "out.json")]
    [InlineData("a.xlsx", "--output", "out.json", "--mensais-only")]
    [InlineData("a.xlsx", "--mensais-only", "--output", "out.json")]
    public void TryParse_MensaisOnlyFlagInAnyPosition_SetsMode(params string[] args)
    {
        var parsed = ImportArguments.TryParse(args, out var arguments, out _);

        parsed.Should().BeTrue();
        arguments.Should().Be(new ImportArguments("a.xlsx", "out.json", MensaisOnly: true));
    }
}
