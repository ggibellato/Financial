using FluentAssertions;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Tests.Persistence;
using Financial.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

[Trait("Category", "Unit")]
public class FxRateLoaderTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    private readonly RecordingLogger<FxRateJsonStore> _logger = new();

    [Fact]
    public void LoadSync_Missing_File_Returns_Empty_Dictionary()
    {
        var storage = new ControllableJsonStorage { ReadException = new FileNotFoundException() };

        var result = FxRateLoader.LoadSync(storage, Serializer, _logger);

        result.Should().BeEmpty();
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_Corrupted_Json_Returns_Empty_Dictionary_And_Logs_The_Error_Type()
    {
        var storage = new ControllableJsonStorage { ReadResult = "not valid json" };

        var result = FxRateLoader.LoadSync(storage, Serializer, _logger);

        result.Should().BeEmpty();
        var warning = _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning).Which;
        warning.Message.Should().Contain("Json").And.NotContain("not valid json");
    }

    [Fact]
    public void LoadSync_Valid_Json_Returns_Populated_Dictionary()
    {
        var storage = new ControllableJsonStorage
        {
            ReadResult = "{\"Version\":1,\"ratesByDate\":{\"2026-09-18\":{\"base\":\"USD\",\"rates\":{\"BRL\":5.45,\"GBP\":0.77},\"source\":\"frankfurter\",\"storedAt\":\"2026-09-18T23:59:59Z\"}}}"
        };

        var result = FxRateLoader.LoadSync(storage, Serializer, _logger);

        result.Should().ContainKey(new DateOnly(2026, 9, 18));
    }
}
