using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Tests.Persistence;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

public class FxRateJsonStoreTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    [Fact]
    public void TryGetRate_Existing_Date_Returns_Stored_Record()
    {
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", DateTimeOffset.UtcNow);
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord> { [new DateOnly(2026, 9, 18)] = record });

        var result = store.TryGetRate(new DateOnly(2026, 9, 18));

        result.Should().Be(record);
    }

    [Fact]
    public void TryGetRate_Missing_Date_Returns_Null()
    {
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord>());

        var result = store.TryGetRate(new DateOnly(2026, 9, 18));

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetRateAsync_Persists_New_Date_To_Storage()
    {
        var storage = new ControllableJsonStorage();
        var store = new FxRateJsonStore(new Dictionary<DateOnly, FxRateRecord>(), storage, Serializer);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", DateTimeOffset.UtcNow);
        var historicalDate = new DateOnly(2026, 1, 1);

        await store.SetRateAsync(historicalDate, record);

        store.TryGetRate(historicalDate).Should().Be(record);
        storage.WrittenJson.Should().ContainSingle(json => json.Contains("\"2026-01-01\""));
    }

    [Fact]
    public async Task SetRateAsync_Rejects_Todays_Date()
    {
        var storage = new ControllableJsonStorage();
        var store = new FxRateJsonStore(new Dictionary<DateOnly, FxRateRecord>(), storage, Serializer);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", DateTimeOffset.UtcNow);

        await store.SetRateAsync(today, record);

        store.TryGetRate(today).Should().BeNull();
        storage.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task SetRateAsync_Two_Dates_In_Quick_Succession_Both_Persisted()
    {
        var storage = new ControllableJsonStorage();
        var store = new FxRateJsonStore(new Dictionary<DateOnly, FxRateRecord>(), storage, Serializer);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", DateTimeOffset.UtcNow);

        await Task.WhenAll(
            store.SetRateAsync(new DateOnly(2026, 1, 1), record),
            store.SetRateAsync(new DateOnly(2026, 1, 2), record));

        store.TryGetRate(new DateOnly(2026, 1, 1)).Should().Be(record);
        store.TryGetRate(new DateOnly(2026, 1, 2)).Should().Be(record);
        storage.WrittenJson.Last().Should().Contain("\"2026-01-01\"").And.Contain("\"2026-01-02\"");
    }

    [Fact]
    public async Task SetRateAsync_Storage_Write_Failure_Does_Not_Throw()
    {
        var storage = new ControllableJsonStorage();
        storage.FailNextWrites(1);
        var store = new FxRateJsonStore(new Dictionary<DateOnly, FxRateRecord>(), storage, Serializer);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", DateTimeOffset.UtcNow);
        var historicalDate = new DateOnly(2026, 1, 1);

        var act = () => store.SetRateAsync(historicalDate, record);

        await act.Should().NotThrowAsync();
        store.TryGetRate(historicalDate).Should().Be(record);
    }

    private static FxRateJsonStore CreateStore(Dictionary<DateOnly, FxRateRecord> seed) =>
        new(seed, new ControllableJsonStorage(), Serializer);
}
