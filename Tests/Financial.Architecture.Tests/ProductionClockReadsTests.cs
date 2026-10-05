using System.IO;
using System.Text.RegularExpressions;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public partial class ProductionClockReadsTests
{
    // Composition roots register the system clock; a control with no composition root reads it directly.
    private static readonly string[] SystemClockAllowedPathFragments =
    [
        "/DependencyInjection/",
        "/Components/MonthYearPicker.xaml.cs",
        "/Persistence/DebouncedJsonStorage.cs",
    ];

    [GeneratedRegex(@"\bDateTime(Offset)?\.(Now|Today|UtcNow)\b")]
    private static partial Regex WallClockRead();

    [GeneratedRegex(@"\bTimeProvider\.System\b")]
    private static partial Regex SystemClockRead();

    [Fact]
    public void Production_Code_Reads_Time_Only_Through_An_Injected_TimeProvider()
    {
        var violations = ProductionSourceFiles()
            .SelectMany(file => FindViolations(file.RelativePath, File.ReadAllText(file.FullPath)))
            .ToList();

        violations.Should().BeEmpty(
            "production time must come from an injected TimeProvider so tests can pin it (docs/rules/implementation.md §Tests)");
    }

    [Theory]
    [InlineData("var x = DateTime" + ".Now;", true)]
    [InlineData("var x = DateTime" + ".UtcNow.Year;", true)]
    [InlineData("var x = DateTimeOffset" + ".UtcNow;", true)]
    [InlineData("var x = DateTime" + ".Today;", true)]
    [InlineData("// DateTime" + ".Now is banned", false)]
    [InlineData("var x = provider.GetUtcNow();", false)]
    [InlineData("var x = new DateTime(2026, 1, 1);", false)]
    public void Scanner_Flags_Wall_Clock_Reads(string line, bool flagged)
    {
        FindViolations("Financial.X/Service.cs", line).Any().Should().Be(flagged);
    }

    [Theory]
    [InlineData("Financial.X/Services/Foo.cs", "var t = timeProvider ?? TimeProvider.System;", true)]
    [InlineData("Financial.X/DependencyInjection/Ext.cs", "services.TryAddSingleton(TimeProvider.System);", false)]
    [InlineData("Financial.App/Components/MonthYearPicker.xaml.cs", "TimeProvider.System.GetLocalNow()", false)]
    public void Scanner_Allows_The_System_Clock_Only_At_The_Composition_Roots(string path, string line, bool flagged)
    {
        FindViolations(path, line).Any().Should().Be(flagged);
    }

    private static IEnumerable<string> FindViolations(string relativePath, string source)
    {
        var normalized = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        var systemClockAllowed = SystemClockAllowedPathFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var code = lines[i].TrimStart();
            if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("///", StringComparison.Ordinal) || code.StartsWith('*'))
            {
                continue;
            }

            if (WallClockRead().IsMatch(code))
            {
                yield return $"{normalized}:{i + 1} reads the wall clock - inject TimeProvider";
            }

            if (!systemClockAllowed && SystemClockRead().IsMatch(code))
            {
                yield return $"{normalized}:{i + 1} uses TimeProvider.System outside a composition root - take a TimeProvider";
            }
        }
    }

    private static IEnumerable<(string RelativePath, string FullPath)> ProductionSourceFiles()
    {
        var root = RepoRoot.Find();
        return Directory.EnumerateDirectories(root, "Financial.*")
            .Where(directory => !Path.GetFileName(directory).EndsWith(".Web", StringComparison.Ordinal))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(path => (Path.GetRelativePath(root, path), path));
    }
}
