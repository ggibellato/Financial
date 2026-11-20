using System.IO;
using System.Text.RegularExpressions;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

[Trait("Category", "Unit")]
public partial class LogRedactionTests
{
    // Tickers are public market symbols, not personal financial data (FallbackFinanceService, DividendsController).
    private static readonly HashSet<string> AllowedPlaceholders =
    [
        "Operation",
        "ErrorType",
        "ExceptionType",
        "PrimaryErrorType",
        "FallbackErrorType",
        "RetryDetail",
        "CreditCardId",
        "Date",
        "Query",
        "Ticker",
        "RequestMethod",
        "RequestRoute",
        "StatusCode",
    ];

    [GeneratedRegex(@"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)?\(\s*(?<template>\$?@?""(?:[^""\\]|\\.)*""(?!""))?")]
    private static partial Regex LogCall();

    [GeneratedRegex(@"\{(?<name>\w+)[^}]*\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void Production_Logs_Carry_Only_The_Operation_Error_Type_And_Allow_Listed_Identifiers()
    {
        var violations = ProductionSourceFiles.Backend()
            .Concat(ProductionSourceFiles.Integrations())
            .SelectMany(file => FindViolations(file.RelativePath, File.ReadAllText(file.FullPath)))
            .ToList();

        violations.Should().BeEmpty(
            "logs must never carry an exception or a value (docs/rules/implementation.md §Public service methods must make failure observable)");
    }

    [Theory]
    [InlineData("_logger.LogWarning(\"{Operation} failed after {ErrorType}\", \"Op\", ex.GetType().Name);", false)]
    [InlineData("_logger.LogInformation(\n    \"{Operation} completed\",\n    \"Op\");", false)]
    [InlineData("_logger.LogError(ex, \"{Operation} failed\", \"Op\");", true)]
    [InlineData("_logger.LogError(\n    ex,\n    \"{Operation} failed\", \"Op\");", true)]
    [InlineData("_logger.LogWarning($\"Failed for {name}\");", true)]
    [InlineData("_logger.LogWarning(\"No asset matches {AssetName}\", request.AssetName);", true)]
    [InlineData("_logger.LogWarning(\"Balance is {Amount:N2}\", amount);", true)]
    [InlineData("_logger.Log(LogLevel.Warning, \"{Operation} failed\", \"Op\");", true)]
    [InlineData("_logger.LogWarning(\"\"\"{Operation} {AssetName}\"\"\", \"Op\", name);", true)]
    [InlineData("// _logger.LogError(ex, \"{Operation} failed\", \"Op\");", false)]
    public void Scanner_Flags_Exceptions_Interpolation_And_Unlisted_Placeholders(string source, bool flagged)
    {
        FindViolations("Financial.X/Service.cs", source).Any().Should().Be(flagged);
    }

    private static IEnumerable<string> FindViolations(string relativePath, string source)
    {
        var normalized = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        foreach (Match call in LogCall().Matches(source))
        {
            var lineStart = source.LastIndexOf('\n', Math.Max(call.Index - 1, 0)) + 1;
            if (source.AsSpan(lineStart, call.Index - lineStart).TrimStart().StartsWith("//"))
            {
                continue;
            }

            var line = source.AsSpan(0, call.Index).Count('\n') + 1;
            var template = call.Groups["template"];
            if (!template.Success)
            {
                yield return $"{normalized}:{line} passes an argument before the message template - log ex.GetType().Name, not the exception";
                continue;
            }

            if (template.Value.StartsWith('$'))
            {
                yield return $"{normalized}:{line} interpolates into the message template - use a named placeholder";
                continue;
            }

            foreach (Match placeholder in Placeholder().Matches(template.Value))
            {
                var name = placeholder.Groups["name"].Value;
                if (!AllowedPlaceholders.Contains(name))
                {
                    yield return $"{normalized}:{line} logs {{{name}}}, which is not an allow-listed identifier";
                }
            }
        }
    }
}
