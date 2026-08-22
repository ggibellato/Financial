using Financial.Shared.Abstractions;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class DebouncedJsonStorage : IJsonStorage, ISyncStatusProvider
{
    private const int DefaultMaxRetries = 5;
    private static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(8);

    private readonly IJsonStorage _inner;
    private readonly TimeSpan _debounceWindow;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxRetries;
    private readonly TimeSpan _flushTimeout;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger _logger;
    private readonly object _lock = new();

    private string? _pendingJson;
    private bool _isDirty;
    private bool _isSaveInFlight;
    private long _generation;
    private Task _currentCycleTask = Task.CompletedTask;
    private SyncState _state = SyncState.Idle;
    private string? _lastError;
    private DateTime? _lastSuccessfulSaveUtc;

    public DebouncedJsonStorage(
        IJsonStorage inner,
        TimeSpan debounceWindow,
        TimeProvider? timeProvider = null,
        ITelemetryTracer? tracer = null,
        ILogger? logger = null)
        : this(inner, debounceWindow, timeProvider, DefaultMaxRetries, DefaultFlushTimeout, tracer, logger)
    {
    }

    internal DebouncedJsonStorage(
        IJsonStorage inner,
        TimeSpan debounceWindow,
        TimeProvider? timeProvider,
        int maxRetries,
        TimeSpan flushTimeout,
        ITelemetryTracer? tracer = null,
        ILogger? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _debounceWindow = debounceWindow;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _maxRetries = maxRetries;
        _flushTimeout = flushTimeout;
        _tracer = tracer ?? NoOpTelemetryTracer.Instance;
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task<string> ReadAsync()
    {
        using var span = _tracer.StartSpan("JsonStorage.Load");
        try
        {
            var json = await _inner.ReadAsync().ConfigureAwait(false);
            span.MarkSuccess();
            return json;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

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
        // The wait runs on the injected provider, which is TimeProvider.System everywhere in
        // production. Tests substitute a controllable clock so they can step the window instead of
        // sleeping against it - racing a real timer is what made these tests flaky in CI.
        // FlushAsync's timeout deliberately stays on the real clock: it is the escape hatch for a
        // save that never completes, so it must fire even when nothing advances the fake clock.
        await Task.Delay(_debounceWindow, _timeProvider).ConfigureAwait(false);

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

        using var span = _tracer.StartSpan("JsonStorage.Save");
        try
        {
            await TransientRetryPolicy.ExecuteWithRetryAsync(
                async () =>
                {
                    await _inner.WriteAsync(jsonToSave).ConfigureAwait(false);
                    return true;
                },
                _maxRetries,
                // The retry policy's message carries only the exception type and retry counters,
                // never document content - safe to log verbatim (logging-audit.md priority 3:
                // a retry firing must be visible in the log stream).
                message => _logger.LogWarning("JsonStorage.Save {RetryDetail}", message)).ConfigureAwait(false);

            span.MarkSuccess();
            HandleSaveSuccess();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            // Exception type only - storage exception messages may embed file paths/identifiers,
            // and the document content itself must never reach the log stream.
            _logger.LogError("JsonStorage.Save failed after retries with {ErrorType}", ex.GetType().Name);
            HandleSaveFailure(ex);
        }
    }

    private void HandleSaveSuccess()
    {
        lock (_lock)
        {
            _isSaveInFlight = false;
            _lastSuccessfulSaveUtc = _timeProvider.GetUtcNow().UtcDateTime;

            if (_isDirty)
            {
                // A write arrived while this save was in flight - start a fresh cycle for it
                // instead of going idle.
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

    private void HandleSaveFailure(Exception ex)
    {
        lock (_lock)
        {
            _isSaveInFlight = false;
            _state = SyncState.Failed;
            _lastError = ex.Message;

            // Deliberately does not auto-start a follow-up cycle even if still dirty - only a
            // subsequent WriteAsync or an explicit FlushAsync re-arms it, avoiding a retry storm
            // against a persistently failing Drive.
        }
    }
}
