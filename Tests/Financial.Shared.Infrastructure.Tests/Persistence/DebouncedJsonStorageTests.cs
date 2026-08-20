using System.Diagnostics;
using Financial.Shared.Abstractions;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Resilience;
using Financial.Shared.Infrastructure.Sync;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

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
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50));

        await storage.WriteAsync("{\"a\":1}");

        await WaitForAsync(() => _inner.WrittenJson.Count == 1);

        _inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
    }

    [Fact]
    public async Task WriteDuringDebounceWindow_ResetsWait_OnlyLatestJsonUploaded()
    {
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(150));

        await storage.WriteAsync("{\"a\":1}");
        await Task.Delay(50);
        await storage.WriteAsync("{\"a\":2}");

        await WaitForAsync(() => _inner.WrittenJson.Count >= 1);
        await Task.Delay(100);

        _inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":2}");
    }

    [Fact]
    public async Task WriteDuringInFlightSave_StartsFollowUpCycleWithoutBlockingTheWrite()
    {
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(30));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        var stopwatch = Stopwatch.StartNew();
        var secondWrite = storage.WriteAsync("{\"a\":2}");
        stopwatch.Stop();

        secondWrite.IsCompleted.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);

        _inner.Release();

        await WaitForAsync(() => _inner.WrittenJson.Count == 2);
        _inner.WrittenJson[0].Should().Be("{\"a\":1}");
        _inner.WrittenJson[1].Should().Be("{\"a\":2}");
    }

    [Fact]
    public async Task RetriesExhausted_StatusBecomesFailed_LastSuccessfulSaveUtcPreserved()
    {
        var fixedTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero));
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), fixedTime, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
        var successTimestamp = storage.GetStatus().LastSuccessfulSaveUtc;
        successTimestamp.Should().NotBeNull();

        _inner.FailNextWrites(1);
        await storage.WriteAsync("{\"a\":2}");

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        var status = storage.GetStatus();
        status.LastError.Should().Contain("Simulated transient storage failure");
        status.LastSuccessfulSaveUtc.Should().Be(successTimestamp);
    }

    [Fact]
    public async Task SaveFailure_DoesNotAutoStartFollowUpCycle()
    {
        _inner.HoldWritesUntilReleased();
        _inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        await storage.WriteAsync("{\"a\":2}");

        _inner.Release();

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);
        await Task.Delay(200);

        _inner.WrittenJson.Should().BeEmpty();
        storage.GetStatus().State.Should().Be(SyncState.Failed);
    }

    [Fact]
    public async Task SuccessfulSave_StatusBecomesIdle_LastSuccessfulSaveUtcUpdates()
    {
        var fixedTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), fixedTime);

        await storage.WriteAsync("{\"a\":1}");

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        storage.GetStatus().LastSuccessfulSaveUtc.Should().Be(fixedTime.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task SuccessfulSave_WhenStillDirtyFromANewerWrite_StatusBecomesPendingNotIdle()
    {
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(300));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        await storage.WriteAsync("{\"a\":2}");

        _inner.Release();

        await WaitForAsync(() => _inner.WrittenJson.Count == 1);

        storage.GetStatus().State.Should().Be(SyncState.Pending);
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
    public async Task FlushAsync_WhenSaveExceedsTimeout_ReturnsWithoutWaitingFurther()
    {
        _inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 5, flushTimeout: TimeSpan.FromMilliseconds(100));

        await storage.WriteAsync("{\"a\":1}");

        var stopwatch = Stopwatch.StartNew();
        await storage.FlushAsync();
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        _inner.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsync_PassesThroughToWrappedStorage()
    {
        _inner.ReadResult = "{\"x\":true}";
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50));

        var result = await storage.ReadAsync();

        result.Should().Be("{\"x\":true}");
    }

    [Fact]
    public async Task TwoInstances_NeverShareDirtyDebounceRetryOrStatusState()
    {
        var failingInner = new ControllableJsonStorage();
        failingInner.FailNextWrites(1);
        var failingStorage = new DebouncedJsonStorage(
            failingInner, TimeSpan.FromMilliseconds(20), null, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        var healthyInner = new ControllableJsonStorage();
        var healthyStorage = new DebouncedJsonStorage(healthyInner, TimeSpan.FromMilliseconds(20));

        await failingStorage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => failingStorage.GetStatus().State == SyncState.Failed);

        healthyStorage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task ReadAsync_RecordsJsonStorageLoadSpan_OnSuccess()
    {
        _inner.ReadResult = "{\"x\":true}";
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(50), tracer: _tracer);

        await storage.ReadAsync();

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("JsonStorage.Load");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        span.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task AfterDebounceWindowElapses_RecordsJsonStorageSaveSpan_OnSuccess()
    {
        var storage = new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(20), tracer: _tracer);

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => _tracer.Spans.Any(s => s.Name == "JsonStorage.Save"));

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name == "JsonStorage.Save").Which;
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task RetriesExhausted_RecordsJsonStorageSaveSpan_OnFailure()
    {
        _inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8), tracer: _tracer);

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => _tracer.Spans.Any(s => s.Name == "JsonStorage.Save"));

        var span = _tracer.Spans.Should().ContainSingle(s => s.Name == "JsonStorage.Save").Which;
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().NotBeNull();
    }

    [Fact]
    public async Task TransientWriteFailure_LogsAWarningForTheRetry_ThenSucceedsWithoutAnError()
    {
        _inner.FailNextWrites(1);
        var logger = new RecordingLogger<DebouncedJsonStorage>();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 5, flushTimeout: TimeSpan.FromSeconds(8), logger: logger);

        await storage.WriteAsync("{\"a\":1}");
        // First attempt fails, the retry policy waits 2s, the second attempt succeeds.
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle, TimeSpan.FromSeconds(10));

        var warning = logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).Which;
        warning.Message.Should().Contain("Retry 1/5");
        warning.Message.Should().Contain(nameof(TransientStorageException));
        warning.Message.Should().NotContain("{\"a\":1}", "document content must never reach the log stream");
        logger.Entries.Should().NotContain(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error);
    }

    [Fact]
    public async Task RetriesExhausted_LogsAnErrorWithTheExceptionTypeOnly()
    {
        _inner.FailNextWrites(1);
        var logger = new RecordingLogger<DebouncedJsonStorage>();
        var storage = new DebouncedJsonStorage(
            _inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8), logger: logger);

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        var error = logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error).Which;
        error.Message.Should().Contain(nameof(TransientStorageException));
        error.Message.Should().NotContain("{\"a\":1}", "document content must never reach the log stream");
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow + effectiveTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Condition not met within {effectiveTimeout}.");
            }

            await Task.Delay(10);
        }
    }
}
