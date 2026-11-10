using System.IO;
using System.Text.RegularExpressions;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Architecture.Tests;

[Trait("Category", "Unit")]
public partial class FixedNameLookupTests
{
    private static readonly string[] WebSourceExtensions = [".ts", ".tsx"];

    private static readonly string[] WebTestPathFragments = ["/__tests__/", "/test/", "/test-utils/", ".test."];

    [GeneratedRegex(@"\bCategory\.ReservaName\b|\bRESERVA_CATEGORY_NAME\b")]
    private static partial Regex FixedNameLookup();

    [Fact]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-06")]
    public void Production_Source_Has_No_Fixed_Name_Lookups()
    {
        var backendFiles = BackendSourceFiles().ToList();
        var webFiles = WebSourceFiles().ToList();

        var violations = backendFiles.Concat(webFiles)
            .SelectMany(file => FindViolations(file.RelativePath, File.ReadAllText(file.FullPath)))
            .ToList();

        backendFiles.Should().NotBeEmpty();
        webFiles.Should().NotBeEmpty();
        violations.Should().BeEmpty("accounts are found by capability or id, never by a fixed name");
    }

    [Theory]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-06")]
    [InlineData("var category = categories.Single(c => c.Name == Category" + ".ReservaName);", true)]
    [InlineData("const reserva = categories.find((c) => c.name === RESERVA" + "_CATEGORY_NAME)", true)]
    [InlineData("var pots = ReservaAccount.Find(accounts);", false)]
    [InlineData("private const string ReservaName = \"Reserva\";", false)]
    public void Scanner_Flags_Fixed_Name_Lookups(string line, bool flagged)
    {
        FindViolations("Financial.X/Service.cs", line).Any().Should().Be(flagged);
    }

    private static IEnumerable<string> FindViolations(string relativePath, string source) =>
        source.Split('\n')
            .Select((line, index) => (line, number: index + 1))
            .Where(entry => FixedNameLookup().IsMatch(entry.line))
            .Select(entry => $"{relativePath.Replace(Path.DirectorySeparatorChar, '/')}:{entry.number} looks an account up by a fixed name");

    private static IEnumerable<(string RelativePath, string FullPath)> BackendSourceFiles()
    {
        var root = RepoRoot.Find();
        return Directory.EnumerateDirectories(root, "Financial.*")
            .Where(directory => !Path.GetFileName(directory).EndsWith(".Web", StringComparison.Ordinal))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .Select(path => (Path.GetRelativePath(root, path), path));
    }

    private static IEnumerable<(string RelativePath, string FullPath)> WebSourceFiles()
    {
        var root = RepoRoot.Find();
        var webSource = Path.Combine(root, "Financial.Web", "src");
        return Directory.EnumerateFiles(webSource, "*", SearchOption.AllDirectories)
            .Where(path => WebSourceExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !WebTestPathFragments.Any(fragment => path.Replace(Path.DirectorySeparatorChar, '/').Contains(fragment, StringComparison.Ordinal)))
            .Select(path => (Path.GetRelativePath(root, path), path));
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");
}
