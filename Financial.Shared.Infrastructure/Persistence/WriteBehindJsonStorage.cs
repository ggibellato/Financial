using Financial.Shared.Infrastructure.Resilience;
using Financial.Shared.Infrastructure.Sync;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class WriteBehindJsonStorage : IJsonStorage, ISyncStatusProvider
{
    private const int DefaultMaxRetries = 5;
    private static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(8);

    private readonly IJsonStorage _inner;
    private readonly TimeSpan _debounceWindow;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxRetries;
    private readonly TimeSpan _flushTimeout;
    private readonly object _lock = new();

    private string? _pendingJson;
    private bool _isDirty;
    private bool _isSaveInFlight;
    private long _generation;
    private Task _currentCycleTask = Task.CompletedTask;
    private SyncState _state = SyncState.Idle;
    private string? _lastError;
    private DateTime? _lastSuccessfulSaveUtc;

    public WriteBehindJsonStorage(IJsonStorage inner, TimeSpan debounceWindow, TimeProvider? timeProvider = null)
        : this(inner, debounceWindow, timeProvider, DefaultMaxRetries, DefaultFlushTimeout)
    {
    }

    internal WriteBehindJsonStorage(
        IJsonStorage inner,
        TimeSpan debounceWindow,
        TimeProvider? timeProvider,
        int maxRetries,
        TimeSpan flushTimeout)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _debounceWindow = debounceWindow;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _maxRetries = maxRetries;
        _flushTimeout = flushTimeout;
    }

    public Task<string> ReadAsync() => _inner.ReadAsync();

    public Task WriteAsync(string json)
    {
        lock (_lock)
        {
            _pendingJson = json;
            _isDirty = true;
            var myGeneration = ++_generation;

            if (!_isSaveInFlight)
            {
                _state = SyncState.Pending;
                _currentCycleTask = Task.Run(() => RunDebounceThenSaveAsync(myGeneration));
            }
        }

        return Task.CompletedTask;
    }

    public SyncStatus GetStatus()
    {
        lock (_lock)
        {
            return new SyncStatus(_state, _lastError, _lastSuccessfulSaveUtc);
        }
    }

    public async Task FlushAsync()
    {
        Task cycleToAwait;
        lock (_lock)
        {
            if (!_isDirty && !_isSaveInFlight)
            {
                return;
            }

            if (_isDirty && !_isSaveInFlight)
            {
                // Invalidate any outstanding debounce wait so it no-ops instead of double-saving.
                _generation++;
                _currentCycleTask = Task.Run(SaveNowAsync);
            }

            cycleToAwait = _currentCycleTask;
        }

        var timeoutTask = Task.Delay(_flushTimeout);
        var completed = await Task.WhenAny(cycleToAwait, timeoutTask).ConfigureAwait(false);

        if (completed == cycleToAwait)
        {
            await cycleToAwait.ConfigureAwait(false);
        }
    }

    private async Task RunDebounceThenSaveAsync(long generation)
    {
        await Task.Delay(_debounceWindow).ConfigureAwait(false);

        lock (_lock)
        {
            if (generation != _generation || _isSaveInFlight)
            {
                return;
            }
        }

        await SaveNowAsync().ConfigureAwait(false);
    }

    private async Task SaveNowAsync()
    {
        string jsonToSave;
        lock (_lock)
        {
            _isSaveInFlight = true;
            _isDirty = false;
            jsonToSave = _pendingJson!;
            _state = SyncState.Saving;
        }

        try
        {
            await TransientRetryPolicy.ExecuteWithRetryAsync(
                async () =>
                {
                    await _inner.WriteAsync(jsonToSave).ConfigureAwait(false);
                    return true;
                },
                _maxRetries).ConfigureAwait(false);

            lock (_lock)
            {
                _isSaveInFlight = false;
                _lastSuccessfulSaveUtc = _timeProvider.GetUtcNow().UtcDateTime;

                if (_isDirty)
                {
                    _state = SyncState.Pending;
                    var myGeneration = ++_generation;
                    _currentCycleTask = Task.Run(() => RunDebounceThenSaveAsync(myGeneration));
                }
                else
                {
                    _state = SyncState.Idle;
                }
            }
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _isSaveInFlight = false;
                _state = SyncState.Failed;
                _lastError = ex.Message;
            }
        }
    }
}
