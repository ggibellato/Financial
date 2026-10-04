using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.CommandLine;

public sealed class LiveDataFileGuardTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cf-guard-" + Guid.NewGuid().ToString("N"));
    private readonly string _toolDirectory;

    public LiveDataFileGuardTests()
    {
        _toolDirectory = Path.Combine(_root, "Tools", "CashFlowSpreadsheetImport", "bin", "Debug");
        Directory.CreateDirectory(_toolDirectory);
        Directory.CreateDirectory(Path.Combine(_root, "data"));
        File.WriteAllText(Path.Combine(_root, "Financial.slnx"), "<Solution />");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void IsLiveDataFile_ExactLivePath_ReturnsTrue()
    {
        var live = Path.Combine(_root, "data", "data-cashflow.json");

        LiveDataFileGuard.IsLiveDataFile(live, _toolDirectory).Should().BeTrue();
    }

    [Fact]
    public void IsLiveDataFile_SamePathWithDifferentCaseAndDotSegments_ReturnsTrue()
    {
        var live = Path.Combine(_root, "data", "..", "DATA", "Data-CashFlow.JSON");

        LiveDataFileGuard.IsLiveDataFile(live, _toolDirectory).Should().BeTrue();
    }

    [Fact]
    public void IsLiveDataFile_TempCopyWithTheSameFileName_ReturnsFalse()
    {
        var copy = Path.Combine(_root, "copy", "data-cashflow.json");

        LiveDataFileGuard.IsLiveDataFile(copy, _toolDirectory).Should().BeFalse();
    }

    [Fact]
    public void IsLiveDataFile_OtherFileInsideDataFolder_ReturnsFalse()
    {
        var scratch = Path.Combine(_root, "data", "scratch.json");

        LiveDataFileGuard.IsLiveDataFile(scratch, _toolDirectory).Should().BeFalse();
    }

    [Fact]
    public void IsLiveDataFile_NoRepositoryRootAboveStartDirectory_ReturnsFalse()
    {
        var isolated = Path.Combine(Path.GetTempPath(), "cf-guard-noroot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolated);
        try
        {
            var live = Path.Combine(_root, "data", "data-cashflow.json");

            LiveDataFileGuard.IsLiveDataFile(live, isolated).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(isolated, recursive: true);
        }
    }
}
