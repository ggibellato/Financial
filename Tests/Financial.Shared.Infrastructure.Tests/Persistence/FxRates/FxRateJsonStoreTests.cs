using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Tests.Persistence;
using FluentAssertions;
using Financial.Shared.Abstractions.Resilience;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Tests.Persistence.FxRates;

[Trait("Category", "Unit")]
public class FxRateJsonStoreTests
{
    private static readonly FxRateSerializerAdapter Serializer = new();

    private readonly RecordingLogger<FxRateJsonStore> _logger = new();

    [Fact]
    public void TryGetRate_Existing_Date_Returns_Stored_Record()
    {
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);
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
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord>(), storage);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);
        var historicalDate = new DateOnly(2026, 1, 1);

        await store.SetRateAsync(historicalDate, record);

        store.TryGetRate(historicalDate).Should().Be(record);
        storage.WrittenJson.Should().ContainSingle(json => json.Contains("\"2026-01-01\""));
    }

    [Fact]
    public async Task SetRateAsync_Rejects_Todays_Date()
    {
        var storage = new ControllableJsonStorage();
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord>(), storage);
        var today = TestClock.Today;
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);

        await store.SetRateAsync(today, record);

        store.TryGetRate(today).Should().BeNull();
        storage.WrittenJson.Should().BeEmpty();
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task SetRateAsync_Two_Dates_In_Quick_Succession_Both_Persisted()
    {
        var storage = new ControllableJsonStorage();
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord>(), storage);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);

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
        var store = CreateStore(new Dictionary<DateOnly, FxRateRecord>(), storage);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);
        var historicalDate = new DateOnly(2026, 1, 1);

        var act = () => store.SetRateAsync(historicalDate, record);

        await act.Should().NotThrowAsync();
        store.TryGetRate(historicalDate).Should().Be(record);
        var warning = _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning).Which;
        warning.Message.Should().Contain(nameof(TransientStorageException)).And.NotContain("5.45");
    }

    private FxRateJsonStore CreateStore(Dictionary<DateOnly, FxRateRecord> seed, ControllableJsonStorage? storage = null) =>
        new(seed, storage ?? new ControllableJsonStorage(), Serializer, TestClock.At(), _logger);
}
