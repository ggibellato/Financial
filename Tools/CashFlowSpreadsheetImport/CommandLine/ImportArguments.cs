using System.Diagnostics.CodeAnalysis;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;

public sealed record ImportArguments(string WorkbookPath, string OutputPath, bool MensaisOnly)
{
    private const string OutputFlag = "--output";
    private const string MensaisOnlyFlag = "--mensais-only";

    private static readonly string UsageLine =
        $"Usage: CashFlowSpreadsheetImport <workbook.xlsx> {OutputFlag} <path> [{MensaisOnlyFlag}]";

    public static readonly string OutputRequiredRefusal =
        "Refusing to run: --output is required and must not be the live data file (data/data-cashflow.json)."
        + Environment.NewLine + UsageLine;

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out ImportArguments? arguments,
        [NotNullWhen(false)] out string? refusal)
    {
        arguments = null;

        string? workbookPath = null;
        string? outputPath = null;
        var mensaisOnly = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == MensaisOnlyFlag)
            {
                mensaisOnly = true;
            }
            else if (arg == OutputFlag)
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    refusal = OutputRequiredRefusal;
                    return false;
                }

                outputPath = args[++i];
            }
            else if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                refusal = $"Unknown option '{arg}'." + Environment.NewLine + UsageLine;
                return false;
            }
            else if (workbookPath is null)
            {
                workbookPath = arg;
            }
            else
            {
                refusal = $"Unexpected argument '{arg}'. Pass the output path with {OutputFlag}." + Environment.NewLine + UsageLine;
                return false;
            }
        }

        if (workbookPath is null || string.IsNullOrWhiteSpace(outputPath))
        {
            refusal = OutputRequiredRefusal;
            return false;
        }

        arguments = new ImportArguments(workbookPath, outputPath, mensaisOnly);
        refusal = null;
        return true;
    }
}
