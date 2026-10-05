using System.Collections.Concurrent;
using System.Diagnostics;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Abstractions.Time;

namespace Financial.Shared.Infrastructure.Persistence.FxRates;

public sealed class FxRateJsonStore : IFxRateStore
{
    private readonly ConcurrentDictionary<DateOnly, FxRateRecord> _ratesByDate;
    private readonly IJsonStorage _storage;
    private readonly IFxRateSerializer _serializer;
    private readonly TimeProvider _timeProvider;

    // SemaphoreSlim, not lock: the critical section awaits storage I/O.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public FxRateJsonStore(
        Dictionary<DateOnly, FxRateRecord> ratesByDate, IJsonStorage storage, IFxRateSerializer serializer, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(ratesByDate);
        _ratesByDate = new ConcurrentDictionary<DateOnly, FxRateRecord>(ratesByDate);
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public FxRateRecord? TryGetRate(DateOnly date) =>
        _ratesByDate.TryGetValue(date, out var record) ? record : null;

    public async Task SetRateAsync(DateOnly date, FxRateRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (date == _timeProvider.GetLocalToday())
        {
            Trace.TraceWarning(
                $"FxRateJsonStore: refused to persist {date:yyyy-MM-dd} - today's rate is intraday and is never stored.");
            return;
        }

        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            _ratesByDate[date] = record;

            try
            {
                var json = _serializer.Serialize(_ratesByDate);
                await _storage.WriteAsync(json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A failed physical write must never fail the caller's rate lookup.
                Trace.TraceWarning(
                    $"FxRateJsonStore: failed to persist rate for {date:yyyy-MM-dd} with {ex.GetType().Name}.");
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public SyncStatus GetStatus() => _storage.GetStatusOrIdle();

    public Task FlushAsync() => _storage.FlushIfSupportedAsync();
}
