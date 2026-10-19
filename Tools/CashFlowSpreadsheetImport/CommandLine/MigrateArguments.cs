using System.Diagnostics.CodeAnalysis;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;

public sealed record MigrateArguments(string InputPath, string OutputPath)
{
    public const string CommandName = "migrate";

    private const string InputFlag = "--input";
    private const string OutputFlag = "--output";

    public static readonly string UsageLine =
        $"Usage: CashFlowSpreadsheetImport {CommandName} {InputFlag} <path> {OutputFlag} <path>";

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out MigrateArguments? arguments,
        [NotNullWhen(false)] out string? refusal)
    {
        arguments = null;
        refusal = UsageLine;

        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is not (InputFlag or OutputFlag) || i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return false;
            }

            if (args[i] == InputFlag)
            {
                inputPath = args[++i];
            }
            else
            {
                outputPath = args[++i];
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        arguments = new MigrateArguments(inputPath, outputPath);
        refusal = null;
        return true;
    }
}
