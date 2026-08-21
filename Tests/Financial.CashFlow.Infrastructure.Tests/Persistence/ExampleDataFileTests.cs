using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

/// <summary>
/// The CashFlow half of the same guard as the Investment example test: the README tells a new install
/// to copy `data/data-cashflow.example.json`, and nothing else in the suite reads it, so it could stop
/// deserializing without a single test noticing. The Investment example had exactly that happen.
/// <para>
/// The file is linked into this project's output by the csproj, so this exercises the tracked file
/// rather than a copy that can drift from it.
/// </para>
/// </summary>
public class ExampleDataFileTests
{
    private static string ExampleFilePath =>
        Path.Combine(AppContext.BaseDirectory, "ExampleData", "data-cashflow.example.json");

    [Fact]
    public void ExampleFile_IsPresentInTheRepository()
    {
        File.Exists(ExampleFilePath).Should().BeTrue(
            "the README tells a new install to copy this file");
    }

    /// <summary>
    /// Unlike the Investment example this one is an empty shell by design - every collection is `[]`,
    /// confirmed as intended when the missing ReserveBuckets seed was raised - so the assertion here is
    /// only that it loads and yields a usable document.
    /// </summary>
    [Fact]
    public void ExampleFile_DeserializesThroughTheRealSerializer()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new CashFlowSerializerAdapter();

        var act = () => CashFlowLoader.LoadSync(storage, serializer);

        act.Should().NotThrow(
            "a fresh checkout seeds from this file, so anything it cannot deserialize stops the app at startup");
    }

    [Fact]
    public void ExampleFile_YieldsAUsableDocument()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new CashFlowSerializerAdapter();

        var data = CashFlowLoader.LoadSync(storage, serializer);

        data.Should().NotBeNull();
    }
}
