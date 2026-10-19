using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.CommandLine;

[Trait("Category", "Integration")]
public sealed class MigrateCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cf-migrate-" + Guid.NewGuid().ToString("N"));
    private readonly string _toolDirectory;
    private readonly string _livePath;
    private readonly string _inputPath;
    private readonly string _outputPath;
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public MigrateCommandTests()
    {
        _toolDirectory = Path.Combine(_root, "Tools", "CashFlowSpreadsheetImport", "bin");
        Directory.CreateDirectory(_toolDirectory);
        Directory.CreateDirectory(Path.Combine(_root, "data"));
        Directory.CreateDirectory(Path.Combine(_root, "copies"));
        File.WriteAllText(Path.Combine(_root, "Financial.slnx"), "<Solution />");
        _livePath = Path.Combine(_root, "data", "data-cashflow.json");
        _inputPath = Path.Combine(_root, "copies", "download.json");
        _outputPath = Path.Combine(_root, "copies", "migrated.json");
    }

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Run_Version1Input_WritesTheMigratedOutputAndReportsIt()
    {
        File.WriteAllText(_inputPath, V1DataFile.Json);

        var exitCode = Run("--input", _inputPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(0);
            File.ReadAllText(_outputPath).Should().Be(DataFileMigrator.CreateDefault().Migrate(V1DataFile.Json, _inputPath).Json);
            File.ReadAllText(_inputPath).Should().Be(V1DataFile.Json);
            _output.ToString().Trim().Should().Be(
                $"Migrated {_inputPath} from version 1 to 2: 3 banks became accounts; 5 references renamed. Wrote {_outputPath}.");
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-02")]
    public void Run_Version2Input_CopiesTheBytesUnchanged()
    {
        File.WriteAllText(_inputPath, V1DataFile.Json);
        Run("--input", _inputPath, "--output", _outputPath);
        var version2Path = Path.Combine(_root, "copies", "version2.json");
        File.Move(_outputPath, version2Path);
        var version2Bytes = File.ReadAllBytes(version2Path);
        _output.GetStringBuilder().Clear();

        var exitCode = Run("--input", version2Path, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(0);
            File.ReadAllBytes(_outputPath).Should().Equal(version2Bytes);
            _output.ToString().Trim().Should().Be($"{version2Path} is already version 2; copied unchanged to {_outputPath}.");
        }
    }

    [Fact]
    public void Run_BadArguments_PrintsUsageAndExits2()
    {
        var exitCode = Run("--input", _inputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(2);
            _error.ToString().Trim().Should().Be(MigrateArguments.UsageLine);
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-03")]
    public void Run_LiveFileAsInput_RefusesAndWritesNothing()
    {
        File.WriteAllText(_livePath, V1DataFile.Json);

        var exitCode = Run("--input", _livePath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be("Refusing to migrate the live data file. Pass a copy.");
            File.Exists(_outputPath).Should().BeFalse();
            File.ReadAllText(_livePath).Should().Be(V1DataFile.Json);
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-03")]
    public void Run_LiveFileAsOutput_RefusesAndWritesNothing()
    {
        File.WriteAllText(_inputPath, V1DataFile.Json);
        File.WriteAllText(_livePath, "live");

        var exitCode = Run("--input", _inputPath, "--output", _livePath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be("Refusing to migrate the live data file. Pass a copy.");
            File.ReadAllText(_livePath).Should().Be("live");
            Directory.GetFiles(Path.Combine(_root, "data")).Should().ContainSingle();
        }
    }

    [Fact]
    public void Run_OutputSameAsInput_Refused()
    {
        File.WriteAllText(_inputPath, V1DataFile.Json);

        var exitCode = Run("--input", _inputPath, "--output", Path.Combine(_root, "copies", "..", "copies", "DOWNLOAD.json"));

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be("--output must be a different file from --input.");
            File.ReadAllText(_inputPath).Should().Be(V1DataFile.Json);
        }
    }

    [Fact]
    public void Run_InputMissing_Refused()
    {
        var exitCode = Run("--input", _inputPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be($"Input file not found: {_inputPath}");
            File.Exists(_outputPath).Should().BeFalse();
        }
    }

    [Fact]
    public void Run_VersionAboveCurrent_RefusedAndWritesNothing()
    {
        File.WriteAllText(_inputPath, """{ "Version": 3 }""");

        var exitCode = Run("--input", _inputPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be($"{_inputPath} is version 3; this build supports up to version 2.");
            File.Exists(_outputPath).Should().BeFalse();
        }
    }

    [Fact]
    public void Run_UnknownBankReference_FailsAndWritesNothing()
    {
        var unknownBankId = Guid.NewGuid().ToString();
        var input = V1DataFile.WithBankReference(unknownBankId);
        File.WriteAllText(_inputPath, input);
        File.WriteAllText(_outputPath, "previous");

        var exitCode = Run("--input", _inputPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(1);
            _error.ToString().Trim().Should().Be(
                $"Migration step V1ToV2BanksToAccounts failed: Expense {V1DataFile.BankExpenseId} references unknown bank {unknownBankId}");
            File.ReadAllText(_outputPath).Should().Be("previous");
            File.ReadAllText(_inputPath).Should().Be(input);
            BackupsOfOutput().Should().BeEmpty();
        }
    }

    [Fact]
    public void Run_OutputExists_BackupCreatedFirst()
    {
        File.WriteAllText(_inputPath, V1DataFile.Json);
        File.WriteAllText(_outputPath, "previous");

        var exitCode = Run("--input", _inputPath, "--output", _outputPath);

        using (new AssertionScope())
        {
            exitCode.Should().Be(0);
            BackupsOfOutput().Should().ContainSingle().Which.Should().Match(backup => File.ReadAllText(backup) == "previous");
        }
    }

    private int Run(params string[] args) =>
        MigrateCommand.Run(args, _toolDirectory, DataFileMigrator.CreateDefault(), _output, _error);

    private string[] BackupsOfOutput() =>
        Directory.GetFiles(Path.Combine(_root, "copies"), "migrated.backup-migration-*");
}
