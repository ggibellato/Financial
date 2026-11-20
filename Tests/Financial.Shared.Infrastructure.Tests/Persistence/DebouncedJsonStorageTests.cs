using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Resilience;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.TestUtilities;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public class DebouncedJsonStorageTests
{
    /// <summary>The wrapped storage and the tracer are the same in every test; only the debounce
    /// window and the retry/flush knobs differ, so those stay on the individual tests.</summary>
    private readonly ControllableJsonStorage _inner;
    private readonly RecordingTelemetryTracer _tracer;

    public DebouncedJsonStorageTests()
    {
        _inner = new ControllableJsonStorage();
        _tracer = new RecordingTelemetryTracer();
    }

    [Fact]
    public async Task WriteAsync_ReturnsImmediately_AndStatusBecomesPending()
    {
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromSeconds(10));

        var writeTask = storage.WriteAsync("{\"a\":1}");

        writeTask.IsCompleted.Should().BeTrue();
        await writeTask;
        storage.GetStatus().State.Should().Be(SyncState.Pending);
        _inner.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task AfterDebounceWindowElapses_LatestJsonIsUploaded()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50), clock);

        await storage.WriteAsync("{\"a\":1}");
        _inner.WrittenJson.Should().BeEmpty();

        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(50));
        await WaitForAsync(() => _inner.WrittenJson.Count == 1);

        _inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
    }

    [Fact]
    public async Task WriteDuringDebounceWindow_ResetsWait_OnlyLatestJsonUploaded()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(150), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(100));

        await storage.WriteAsync("{\"a\":2}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(100));

        // 200ms have now passed since the first write, so its 150ms window would have elapsed had
        // the second write not restarted it.
        _inner.WrittenJson.Should().BeEmpty();

        clock.Advance(TimeSpan.FromMilliseconds(60));
        await WaitForAsync(() => _inner.WrittenJson.Count == 1);

        // The whole upload sequence, not a sample of it: had the wait not reset, the first window
        // would have uploaded {"a":1} on its own and this would read ["{\"a\":1}", "{\"a\":2}"].
        _inner.WrittenJson.Should().Equal("{\"a\":2}");
    }

    [Fact]
    public async Task WriteDuringInFlightSave_StartsFollowUpCycleWithoutBlockingTheWrite()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(30), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(30));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        var secondWrite = storage.WriteAsync("{\"a\":2}");

        secondWrite.IsCompleted.Should().BeTrue();

        _inner.Release();
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(30));

        await WaitForAsync(() => _inner.WrittenJson.Count == 2);
        _inner.WrittenJson[0].Should().Be("{\"a\":1}");
        _inner.WrittenJson[1].Should().Be("{\"a\":2}");
    }

    [Fact]
    public async Task RetriesExhausted_StatusBecomesFailed_LastSuccessfulSaveUtcPreserved()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
        var successTimestamp = storage.GetStatus().LastSuccessfulSaveUtc;
        successTimestamp.Should().NotBeNull();

        _inner.FailNextWrites(1);
        await storage.WriteAsync("{\"a\":2}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        var status = storage.GetStatus();
        status.LastError.Should().Contain("Simulated transient storage failure");
        status.LastSuccessfulSaveUtc.Should().Be(successTimestamp);
    }

    [Fact]
    public async Task SaveFailure_DoesNotAutoStartFollowUpCycle()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        _inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        await storage.WriteAsync("{\"a\":2}");

        _inner.Release();

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        clock.TimersArmed.Should().Be(1, "a failed save must not arm a follow-up debounce timer");
        _inner.WrittenJson.Should().BeEmpty();
        storage.GetStatus().State.Should().Be(SyncState.Failed);
    }

    [Fact]
    public async Task SuccessfulSave_StatusBecomesIdle_LastSuccessfulSaveUtcUpdates()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        storage.GetStatus().LastSuccessfulSaveUtc.Should().Be(clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task SuccessfulSave_WhenStillDirtyFromANewerWrite_StatusBecomesPendingNotIdle()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(300), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(300));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        // Dirties the document again while the first save is still blocked on the gate.
        await storage.WriteAsync("{\"a\":2}");
        _inner.Release();

        await WaitForAsync(() => _inner.WrittenJson.Count == 1);

        // Pending is a resting state here, not a moment to be caught: the follow-up cycle waits on
        // the fake clock, so it cannot advance to Saving until this test says so. That is what the
        // old version raced - it sampled the status and lost when the runner stalled past 300ms.
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Pending);

        // Then drive the rest of the sequence to prove Pending meant "a newer document is still
        // queued", rather than the save cycle having quietly stalled.
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(300));
        await WaitForAsync(() => _inner.WrittenJson.Count == 2);
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        _inner.WrittenJson.Should().Equal("{\"a\":1}", "{\"a\":2}");
    }

    [Fact]
    public async Task FlushAsync_OnDirtyInstance_SavesImmediatelyWithoutWaitingForDebounce()
    {
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromSeconds(30));

        await storage.WriteAsync("{\"a\":1}");
        _inner.WrittenJson.Should().BeEmpty();

        await storage.FlushAsync();

        _inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":1}");
        storage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task FlushAsync_CancelsThePendingDebounce_SoTheDocumentIsSavedOnce()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock);

        await storage.WriteAsync("{\"a\":1}");
        await storage.FlushAsync();
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));

        _inner.WrittenJson.Should().Equal("{\"a\":1}");
        storage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task WriteAfterAFlush_IsNotSavedEarlyByTheDebounceTheFlushCancelled()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock);

        await storage.WriteAsync("{\"a\":1}");
        await storage.FlushAsync();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        await storage.WriteAsync("{\"a\":2}");
        await WaitForAsync(() => clock.TimersArmed >= 3);
        clock.Advance(TimeSpan.FromMilliseconds(10));

        _inner.WrittenJson.Should().Equal("{\"a\":1}");

        clock.Advance(TimeSpan.FromMilliseconds(10));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        _inner.WrittenJson.Should().Equal("{\"a\":1}", "{\"a\":2}");
    }

    [Fact]
    public async Task FlushAsync_DuringAnInFlightSaveWithNoNewerWrite_WaitsForIt()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        var flush = storage.FlushAsync();
        flush.IsCompleted.Should().BeFalse();
        _inner.Release();
        await flush;

        _inner.WrittenJson.Should().Equal("{\"a\":1}");
        storage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task FlushAsync_DuringAnInFlightSave_WaitsForItInsteadOfStartingASecondSave()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);
        await storage.WriteAsync("{\"a\":2}");

        var flush = storage.FlushAsync();
        flush.IsCompleted.Should().BeFalse();
        _inner.Release();
        await flush;

        _inner.WrittenJson.Should().Equal("{\"a\":1}");
        storage.GetStatus().State.Should().Be(SyncState.Pending);
    }

    [Fact]
    public async Task WriteAfterAFailedSave_StartsANewSave()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        await storage.WriteAsync("{\"a\":2}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        _inner.WrittenJson.Should().Equal("{\"a\":2}");
    }

    [Fact]
    public async Task FlushAsync_WhenSaveExceedsTimeout_ReturnsWithoutWaitingFurther()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 5, flushTimeout: TimeSpan.FromMilliseconds(100));

        await storage.WriteAsync("{\"a\":1}");
        var flush = storage.FlushAsync();
        await WaitForAsync(() => clock.TimersArmed >= 2);
        flush.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromMilliseconds(100));
        await flush;

        _inner.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task TwoInstances_NeverShareDirtyDebounceRetryOrStatusState()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var failingInner = new ControllableJsonStorage();
        failingInner.FailNextWrites(1);
        var failingStorage = new DebouncedJsonStorage(
            failingInner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        var healthyInner = new ControllableJsonStorage();
        var healthyStorage = new DebouncedJsonStorage(healthyInner, TimeSpan.FromMilliseconds(20), clock);

        await failingStorage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => failingStorage.GetStatus().State == SyncState.Failed);

        healthyStorage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task ReadAsync_RecordsJsonStorageLoadSpan_OnSuccess()
    {
        _inner.ReadResult = "{\"x\":true}";
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50), tracer: _tracer);

        var result = await storage.ReadAsync();

        result.Should().Be("{\"x\":true}");
        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("JsonStorage.Load");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        span.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_WhenWrappedStorageThrows_RethrowsAndRecordsAFailedLoadSpan()
    {
        _inner.ReadException = new InvalidOperationException("boom");
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50), tracer: _tracer);

        var act = () => storage.ReadAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        var span = _tracer.Spans.Should().ContainSingle(s => s.Name == "JsonStorage.Load").Which;
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().BeSameAs(_inner.ReadException);
    }

    [Fact]
    public async Task AfterDebounceWindowElapses_RecordsJsonStorageSaveSpan_OnSuccess()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), clock, tracer: _tracer);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => _tracer.Spans.Any(s => s.Name == "JsonStorage.Save" && s.Disposed));

        _tracer.ShouldHaveSucceededSpan("JsonStorage.Save");
    }

    [Fact]
    public async Task RetriesExhausted_RecordsJsonStorageSaveSpan_OnFailure()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8), tracer: _tracer);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => _tracer.Spans.Any(s => s.Name == "JsonStorage.Save" && s.Disposed));

        _tracer.ShouldHaveFailedSpan<TransientStorageException>("JsonStorage.Save");
    }

    [Fact]
    public async Task TransientWriteFailure_LogsAWarningForTheRetry_ThenSucceedsWithoutAnError()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.FailNextWrites(1);
        var logger = new RecordingLogger<DebouncedJsonStorage>();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 5, flushTimeout: TimeSpan.FromSeconds(8), logger: logger);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await FireNextTimerAsync(clock, TimeSpan.FromSeconds(2));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        var warning = logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).Which;
        warning.Message.Should().Contain("Retry 1/5");
        warning.Message.Should().Contain(nameof(TransientStorageException));
        warning.Message.Should().NotContain("{\"a\":1}", "document content must never reach the log stream");
        logger.Entries.Should().NotContain(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error);
    }

    [Fact]
    public async Task RetriesExhausted_LogsAnErrorWithTheExceptionTypeOnly()
    {
        var clock = new ObservableFakeClock(TestClock.Default);
        _inner.FailNextWrites(1);
        var logger = new RecordingLogger<DebouncedJsonStorage>();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), clock, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8), logger: logger);

        await storage.WriteAsync("{\"a\":1}");
        await FireNextTimerAsync(clock, TimeSpan.FromMilliseconds(20));
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        var error = logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error).Which;
        error.Message.Should().Contain(nameof(TransientStorageException));
        error.Message.Should().NotContain("{\"a\":1}", "document content must never reach the log stream");
    }

    private static async Task FireNextTimerAsync(ObservableFakeClock clock, TimeSpan span)
    {
        var expected = ++clock.TimersFired;
        await WaitForAsync(() => clock.TimersArmed >= expected);
        clock.Advance(span);
    }

    private static Task WaitForAsync(Func<bool> condition) => Eventually.HoldsAsync(condition);
}
