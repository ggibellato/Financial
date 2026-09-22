using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

public class FxRateLoaderTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    [Fact]
    public void LoadSync_Missing_File_Returns_Empty_Dictionary()
    {
        var storage = new ThrowingJsonStorage(new FileNotFoundException());

        var result = FxRateLoader.LoadSync(storage, Serializer);

        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_Corrupted_Json_Returns_Empty_Dictionary()
    {
        var storage = new FixedJsonStorage("not valid json");

        var act = () => FxRateLoader.LoadSync(storage, Serializer);

        act.Should().NotThrow();
        FxRateLoader.LoadSync(storage, Serializer).Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_Valid_Json_Returns_Populated_Dictionary()
    {
        var storage = new FixedJsonStorage(
            "{\"Version\":1,\"ratesByDate\":{\"2026-09-18\":{\"base\":\"USD\",\"rates\":{\"BRL\":5.45,\"GBP\":0.77},\"source\":\"frankfurter\",\"storedAt\":\"2026-09-18T23:59:59Z\"}}}");

        var result = FxRateLoader.LoadSync(storage, Serializer);

        result.Should().ContainKey(new DateOnly(2026, 9, 18));
    }

    private sealed class FixedJsonStorage(string json) : IJsonStorage
    {
        public Task<string> ReadAsync() => Task.FromResult(json);
        public Task WriteAsync(string json) => Task.CompletedTask;
    }

    private sealed class ThrowingJsonStorage(Exception exception) : IJsonStorage
    {
        public Task<string> ReadAsync() => throw exception;
        public Task WriteAsync(string json) => Task.CompletedTask;
    }
}
