using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;

public static class MigrateCommand
{
    public const string LiveFileRefusal = "Refusing to migrate the live data file. Pass a copy.";
    public const string SameFileRefusal = "--output must be a different file from --input.";

    public static int Run(string[] args, string startDirectory, DataFileMigrator migrator, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(migrator);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!MigrateArguments.TryParse(args, out var arguments, out var refusal))
        {
            error.WriteLine(refusal);
            return 2;
        }

        var (inputPath, outputPath) = arguments;

        if (LiveDataFileGuard.IsLiveDataFile(inputPath, startDirectory) || LiveDataFileGuard.IsLiveDataFile(outputPath, startDirectory))
        {
            error.WriteLine(LiveFileRefusal);
            return 1;
        }

        if (LiveDataFileGuard.IsSamePath(inputPath, outputPath))
        {
            error.WriteLine(SameFileRefusal);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            error.WriteLine($"Input file not found: {inputPath}");
            return 1;
        }

        DataFileMigrationResult result;
        try
        {
            result = migrator.Migrate(File.ReadAllText(inputPath), inputPath);
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine(ex.Message);
            return 1;
        }

        if (File.Exists(outputPath))
        {
            output.WriteLine($"Backed up {outputPath} to {MigrationBackup.Create(outputPath)}.");
        }

        if (result.WasAlreadyCurrent)
        {
            File.Copy(inputPath, outputPath, overwrite: true);
            output.WriteLine($"{inputPath} is already version {result.ToVersion}; copied unchanged to {outputPath}.");
            return 0;
        }

        File.WriteAllText(outputPath, result.Json);
        output.WriteLine(
            $"Migrated {inputPath} from version {result.FromVersion} to {result.ToVersion}: {string.Join("; ", result.StepSummaries)}. Wrote {outputPath}.");
        return 0;
    }
}
