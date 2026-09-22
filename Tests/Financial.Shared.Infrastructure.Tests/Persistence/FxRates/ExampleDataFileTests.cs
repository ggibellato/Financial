using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

/// <summary>
/// Same guard as the Investment/CashFlow example-data tests: the README tells a new install to
/// copy `data/data-fx-rates.example.json`, and nothing else in the suite reads it, so it could
/// stop deserializing without a single test noticing.
/// </summary>
public class ExampleDataFileTests
{
    private static string ExampleFilePath =>
        Path.Combine(AppContext.BaseDirectory, "ExampleData", "data-fx-rates.example.json");

    [Fact]
    public void ExampleFile_IsPresentInTheRepository()
    {
        File.Exists(ExampleFilePath).Should().BeTrue("the README tells a new install to copy this file");
    }

    [Fact]
    public void ExampleFile_DeserializesThroughTheRealSerializer()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new FxRateSerializerAdapter();

        var act = () => FxRateLoader.LoadSync(storage, serializer);

        act.Should().NotThrow("a fresh checkout seeds from this file, so anything it cannot deserialize stops the app at startup");
    }

    [Fact]
    public void ExampleFile_YieldsAnEmptyStore()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new FxRateSerializerAdapter();

        var ratesByDate = FxRateLoader.LoadSync(storage, serializer);

        ratesByDate.Should().BeEmpty();
    }
}
