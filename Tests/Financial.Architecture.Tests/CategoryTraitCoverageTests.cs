using System.IO;
using System.Text.RegularExpressions;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Architecture.Tests;

[Trait("Category", "Unit")]
public partial class CategoryTraitCoverageTests
{
    private static readonly string[] AllowedCategories = ["Unit", "Integration", "Live", "E2E", "Smoke"];

    [GeneratedRegex(@"^[ \t]*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|file)\s+)*(?:record\s+)?class\s+(\w+)", RegexOptions.Multiline)]
    private static partial Regex ClassDeclaration();

    [GeneratedRegex(@"^[ \t]*\[(?:Fact|Theory)\b", RegexOptions.Multiline)]
    private static partial Regex TestAttribute();

    [GeneratedRegex(@"Trait\(\s*""Category""\s*,\s*""(?<value>[^""]*)""\s*\)")]
    private static partial Regex CategoryTrait();

    [Fact]
    public void Every_Test_Class_Declares_A_Category_Trait()
    {
        var violations = FindViolations(TestSources());

        violations.Should().BeEmpty(
            "every test class must declare [Trait(\"Category\", ...)] so CI and local runs can filter by level (docs/rules/implementation.md §Tests)");
    }

    [Fact]
    public void Scanner_Flags_A_Test_Class_Without_A_Category()
    {
        var sources = Sources(("A/FooTests.cs", "public class FooTests\n{\n    [Fact]\n    public void Works() { }\n}\n"));

        FindViolations(sources).Should().ContainSingle().Which.Should().Contain("A/FooTests.cs:1").And.Contain("FooTests");
    }

    [Fact]
    public void Scanner_Accepts_A_Class_With_A_Valid_Category()
    {
        var sources = Sources(("A/FooTests.cs", "[Trait(\"Category\", \"Unit\")]\npublic class FooTests\n{\n    [Fact]\n    public void Works() { }\n}\n"));

        FindViolations(sources).Should().BeEmpty();
    }

    [Fact]
    public void Scanner_Rejects_An_Unknown_Category_Value()
    {
        var sources = Sources(("A/FooTests.cs", "[Trait(\"Category\", \"Fast\")]\npublic class FooTests\n{\n    [Fact]\n    public void Works() { }\n}\n"));

        FindViolations(sources).Should().ContainSingle().Which.Should().Contain("Fast");
    }

    [Fact]
    public void Scanner_Accepts_A_Class_Inheriting_The_Category_From_Its_Base()
    {
        var sources = Sources(
            ("A/Base.cs", "[Trait(\"Category\", \"Integration\")]\npublic abstract class ApiBase\n{\n    [Fact]\n    public void Shared() { }\n}\n"),
            ("A/FooTests.cs", "public class FooTests : ApiBase\n{\n    [Fact]\n    public void Works() { }\n}\n"));

        FindViolations(sources).Should().BeEmpty();
    }

    [Fact]
    public void Scanner_Checks_Nested_Test_Classes_Separately()
    {
        var sources = Sources(("A/FooTests.cs",
            "[Trait(\"Category\", \"Unit\")]\npublic class FooTests\n{\n    [Fact]\n    public void Works() { }\n\n    public class Inner\n    {\n        [Fact]\n        public void Nested() { }\n    }\n}\n"));

        FindViolations(sources).Should().ContainSingle().Which.Should().Contain("Inner");
    }

    [Fact]
    public void Scanner_Ignores_Classes_Without_Tests()
    {
        var sources = Sources(("A/Helper.cs", "public class Helper\n{\n    public void Run() { }\n}\n"));

        FindViolations(sources).Should().BeEmpty();
    }

    private static IReadOnlyDictionary<string, string> Sources(params (string Path, string Source)[] files) =>
        files.ToDictionary(file => file.Path, file => file.Source);

    private static IReadOnlyDictionary<string, string> TestSources()
    {
        var root = RepoRoot.Find();
        var separator = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(Path.Combine(root, "Tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}obj{separator}") && !path.Contains($"{separator}bin{separator}"))
            .ToDictionary(path => Path.GetRelativePath(root, path).Replace(separator, '/'), File.ReadAllText);
    }

    private static List<string> FindViolations(IReadOnlyDictionary<string, string> sources)
    {
        var classes = sources.SelectMany(file => ParseClasses(file.Key, file.Value)).ToList();
        var byName = classes.ToLookup(c => c.Name);

        return classes
            .Where(c => c.TestCount > 0)
            .Select(c => Describe(c, byName))
            .OfType<string>()
            .ToList();
    }

    private static string? Describe(TestClass testClass, ILookup<string, TestClass> byName)
    {
        var categories = CategoriesOf(testClass, byName, []);
        if (categories.Count == 0)
        {
            return $"{testClass.File}:{testClass.Line} {testClass.Name} has no [Trait(\"Category\", ...)]; use Unit, Integration or Live";
        }

        var unknown = categories.FirstOrDefault(value => !AllowedCategories.Contains(value));
        return unknown is null
            ? null
            : $"{testClass.File}:{testClass.Line} {testClass.Name} uses unknown Category \"{unknown}\"; allowed: {string.Join(", ", AllowedCategories)}";
    }

    private static List<string> CategoriesOf(TestClass testClass, ILookup<string, TestClass> byName, HashSet<string> visited)
    {
        if (testClass.Categories.Count > 0 || !visited.Add(testClass.Name))
        {
            return testClass.Categories;
        }

        return testClass.Bases
            .SelectMany(baseName => byName[baseName])
            .Select(baseClass => CategoriesOf(baseClass, byName, visited))
            .FirstOrDefault(found => found.Count > 0) ?? [];
    }

    private static IEnumerable<TestClass> ParseClasses(string file, string source)
    {
        var declarations = new List<(string Name, int Start, int BodyStart, int BodyEnd, List<string> Bases, List<string> Categories, int Line)>();
        foreach (Match match in ClassDeclaration().Matches(source))
        {
            var bodyStart = source.IndexOf('{', match.Index + match.Length);
            if (bodyStart < 0)
            {
                continue;
            }

            declarations.Add((
                match.Groups[1].Value,
                match.Index,
                bodyStart,
                MatchingBrace(source, bodyStart),
                BaseTypes(source[(match.Index + match.Length)..bodyStart]),
                AttributeCategories(source, match.Index),
                source.AsSpan(0, match.Index).Count('\n') + 1));
        }

        var counts = new int[declarations.Count];
        foreach (Match attribute in TestAttribute().Matches(source))
        {
            var owner = -1;
            for (var i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                var contains = declaration.BodyStart < attribute.Index && attribute.Index < declaration.BodyEnd;
                if (contains && (owner < 0 || declaration.BodyStart > declarations[owner].BodyStart))
                {
                    owner = i;
                }
            }

            if (owner >= 0)
            {
                counts[owner]++;
            }
        }

        return declarations.Select((d, i) => new TestClass(file, d.Line, d.Name, d.Bases, d.Categories, counts[i]));
    }

    private static int MatchingBrace(string source, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}' && --depth == 0)
            {
                return i;
            }
        }

        return source.Length;
    }

    private static List<string> BaseTypes(string header)
    {
        var afterParameters = header.Contains(')') ? header[(header.LastIndexOf(')') + 1)..] : header;
        var colon = afterParameters.IndexOf(':');
        if (colon < 0)
        {
            return [];
        }

        var list = Regex.Replace(afterParameters[(colon + 1)..], @"\bwhere\b.*", string.Empty, RegexOptions.Singleline);
        list = Regex.Replace(list, "<[^>]*>", string.Empty);
        return Regex.Matches(list, @"[A-Za-z_]\w*").Select(m => m.Value).ToList();
    }

    private static List<string> AttributeCategories(string source, int classIndex)
    {
        var lineStart = source.LastIndexOf('\n', Math.Max(classIndex - 1, 0)) + 1;
        var categories = new List<string>();
        var cursor = lineStart;
        while (cursor > 0)
        {
            var previousStart = source.LastIndexOf('\n', Math.Max(cursor - 2, 0)) + 1;
            var line = source[previousStart..cursor].Trim();
            if (!line.StartsWith('['))
            {
                break;
            }

            categories.AddRange(CategoryTrait().Matches(line).Select(m => m.Groups["value"].Value));
            cursor = previousStart;
        }

        return categories;
    }

    private sealed record TestClass(string File, int Line, string Name, List<string> Bases, List<string> Categories, int TestCount);
}
