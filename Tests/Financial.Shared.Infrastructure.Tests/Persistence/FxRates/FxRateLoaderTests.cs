using FluentAssertions;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Tests.Persistence;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

public class FxRateLoaderTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    [Fact]
    public void LoadSync_Missing_File_Returns_Empty_Dictionary()
    {
        var storage = new ControllableJsonStorage { ReadException = new FileNotFoundException() };

        var result = FxRateLoader.LoadSync(storage, Serializer);

        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_Corrupted_Json_Returns_Empty_Dictionary()
    {
        var storage = new ControllableJsonStorage { ReadResult = "not valid json" };

        var act = () => FxRateLoader.LoadSync(storage, Serializer);

        act.Should().NotThrow();
        FxRateLoader.LoadSync(storage, Serializer).Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_Valid_Json_Returns_Populated_Dictionary()
    {
        var storage = new ControllableJsonStorage
        {
            ReadResult = "{\"Version\":1,\"ratesByDate\":{\"2026-09-18\":{\"base\":\"USD\",\"rates\":{\"BRL\":5.45,\"GBP\":0.77},\"source\":\"frankfurter\",\"storedAt\":\"2026-09-18T23:59:59Z\"}}}"
        };

        var result = FxRateLoader.LoadSync(storage, Serializer);

        result.Should().ContainKey(new DateOnly(2026, 9, 18));
    }
}
