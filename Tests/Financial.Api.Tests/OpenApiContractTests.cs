using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Financial.Api.Tests;

/// <summary>
/// Pins the shape of the public API by comparing the generated OpenAPI document against a committed
/// snapshot.
/// <para>
/// Every domain-facing controller returns an Application DTO straight over the wire, so reshaping one
/// is a wire-format break. Nothing caught that before: the endpoint tests deserialize into the very
/// type that would change (<c>ReadFromJsonAsync&lt;AssetDetailsDTO&gt;</c> and friends), so a renamed
/// property moves the endpoint and its test together and the suite stays green while the SPA breaks at
/// runtime. This test is the thing that notices.
/// </para>
/// <para>
/// It is not a substitute for an API-owned contract layer (backlog §4.2) - Application DTOs still cross
/// the wire. It removes the silence: a reshape now fails the build naming the endpoint, and the
/// committed document is also the artefact <c>types.ts</c> can eventually be generated from (§4.4).
/// </para>
/// </summary>
public class OpenApiContractTests
{
    /// <summary>
    /// Set to any non-empty value to rewrite the snapshot instead of asserting against it.
    /// See CLAUDE.md for the PowerShell and bash invocations.
    /// </summary>
    private const string UpdateFlag = "UPDATE_OPENAPI_SNAPSHOT";

    [Fact]
    public async Task OpenApiDocument_MatchesTheCommittedSnapshot()
    {
        var current = Canonicalize(await FetchDocumentAsync());
        var snapshotPath = ResolveSnapshotPath();

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(UpdateFlag)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, current, Encoding.UTF8);
            return;
        }

        File.Exists(snapshotPath).Should().BeTrue(
            $"the API contract snapshot should be committed at {snapshotPath}. Regenerate it by setting " +
            $"{UpdateFlag}=1 and running: dotnet test Tests/Financial.Api.Tests");

        var committed = await File.ReadAllTextAsync(snapshotPath, Encoding.UTF8);

        // Asserting on the raw strings would dump ~200KB of JSON into the build log, which nobody can
        // read. Report the lines that actually differ instead.
        var differences = DescribeDifferences(committed, current);

        differences.Should().BeNullOrEmpty(
            "the public API shape changed. If the change is intended, regenerate the snapshot by setting " +
            $"{UpdateFlag}=1 and running: dotnet test Tests/Financial.Api.Tests. Review the diff - anything removed " +
            "or renamed is a breaking change for Financial.Web, whose types.ts is hand-written and will " +
            "not fail to compile against it");
    }

    /// <summary>
    /// Both documents are canonicalized - property names sorted, one token per line - so a contract
    /// change shows up as whole lines appearing or disappearing. Reporting those lines is enough to
    /// see what moved without printing the entire document.
    /// </summary>
    private static string? DescribeDifferences(string committed, string current)
    {
        if (committed == current)
        {
            return null;
        }

        const int MaxReported = 15;
        var committedLines = SplitLines(committed);
        var currentLines = SplitLines(current);

        var removed = Subtract(committedLines, currentLines);
        var added = Subtract(currentLines, committedLines);

        var report = new StringBuilder();
        report.AppendLine($"{removed.Count} line(s) gone from the contract, {added.Count} added.");
        Describe(report, "GONE (breaking for Financial.Web if a field or path)", removed, MaxReported);
        Describe(report, "ADDED", added, MaxReported);

        return report.ToString();
    }

    private static List<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();

    /// <summary>Multiset difference, so a line occurring three times and now twice still registers.</summary>
    private static List<string> Subtract(List<string> from, List<string> other)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in other)
        {
            counts[line] = counts.TryGetValue(line, out var n) ? n + 1 : 1;
        }

        var result = new List<string>();
        foreach (var line in from)
        {
            if (counts.TryGetValue(line, out var n) && n > 0)
            {
                counts[line] = n - 1;
                continue;
            }

            result.Add(line);
        }

        return result;
    }

    private static void Describe(StringBuilder report, string heading, List<string> lines, int max)
    {
        if (lines.Count == 0)
        {
            return;
        }

        report.AppendLine(heading + ":");
        foreach (var line in lines.Take(max))
        {
            report.AppendLine("  " + line);
        }

        if (lines.Count > max)
        {
            report.AppendLine($"  ... and {lines.Count - max} more");
        }
    }

    private static async Task<string> FetchDocumentAsync()
    {
        // The document is only mapped in Development, which is also what SwaggerEndpointsTests relies on.
        await using var factory = new ApiTestFactory().WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Re-serializes with property names sorted and indented, so the snapshot does not churn on
    /// incidental ordering from route discovery and a real change reads as a readable diff.
    /// </summary>
    private static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            WriteSorted(document.RootElement, writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSorted(property.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSorted(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// Resolves the snapshot in the project directory rather than the build output, so the regenerate
    /// path writes the file that is actually committed.
    /// </summary>
    private static string ResolveSnapshotPath() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Contract", "openapi-v1.snapshot.json"));
}
