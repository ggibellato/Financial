using System.IO;
using System.Linq;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Persistence;

/// <summary>
/// The README's first-run instruction is "copy `data/data-investment.example.json` to
/// `data/data-investment.json`", so this file is the seed a fresh checkout starts from. Nothing else
/// loads it: every other test seeds from `TestData/data.test.json`, and CI seeds its own fixture, so
/// the example could stop deserializing and every suite would still pass. It did - a
/// <c>"Country": ""</c> made the host throw on startup, and it was found only by hand-booting a
/// published build.
/// <para>
/// The file is linked into this project's output by the csproj, so this exercises the tracked file
/// itself rather than a copy that can drift from it.
/// </para>
/// </summary>
public class ExampleDataFileTests
{
    private static string ExampleFilePath =>
        Path.Combine(AppContext.BaseDirectory, "ExampleData", "data-investment.example.json");

    [Fact]
    public void ExampleFile_IsPresentInTheRepository()
    {
        File.Exists(ExampleFilePath).Should().BeTrue(
            "the README tells a new install to copy this file");
    }

    [Fact]
    public void ExampleFile_DeserializesThroughTheRealSerializer()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new InvestmentsSerializerAdapter();

        var act = () => InvestmentsLoader.LoadSync(storage, serializer);

        act.Should().NotThrow(
            "a fresh checkout seeds from this file, so anything it cannot deserialize stops the app at startup");
    }

    /// <summary>
    /// Loading is not enough on its own: an empty document would deserialize and prove nothing about
    /// the shape a new install actually receives.
    /// </summary>
    [Fact]
    public void ExampleFile_ContainsAWorkedExampleRatherThanAnEmptyShell()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new InvestmentsSerializerAdapter();

        var investments = InvestmentsLoader.LoadSync(storage, serializer);

        investments.ActiveBrokers.Should().NotBeEmpty();
        investments.ActiveBrokers.SelectMany(b => b.Portfolios).SelectMany(p => p.Assets).Should().NotBeEmpty();
    }
}
