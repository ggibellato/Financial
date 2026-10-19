using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.CommandLine;

[Trait("Category", "Unit")]
public class MigrateArgumentsTests
{
    [Theory]
    [InlineData("--input", "in.json", "--output", "out.json")]
    [InlineData("--output", "out.json", "--input", "in.json")]
    public void TryParse_InputAndOutput_ReturnsBothPaths(params string[] args)
    {
        var parsed = MigrateArguments.TryParse(args, out var arguments, out var refusal);

        using (new AssertionScope())
        {
            parsed.Should().BeTrue();
            arguments.Should().Be(new MigrateArguments("in.json", "out.json"));
            refusal.Should().BeNull();
        }
    }

    [Theory]
    [InlineData]
    [InlineData("--input", "in.json")]
    [InlineData("--output", "out.json")]
    [InlineData("--input", "--output", "out.json")]
    [InlineData("--input", "in.json", "--output")]
    [InlineData("--input", "in.json", "--output", "out.json", "--force")]
    [InlineData("--input", "in.json", "--output", "out.json", "extra")]
    public void TryParse_MissingOrUnknownArguments_RefusesWithTheUsageLine(params string[] args)
    {
        var parsed = MigrateArguments.TryParse(args, out var arguments, out var refusal);

        using (new AssertionScope())
        {
            parsed.Should().BeFalse();
            arguments.Should().BeNull();
            refusal.Should().Be("Usage: CashFlowSpreadsheetImport migrate --input <path> --output <path>");
        }
    }
}
