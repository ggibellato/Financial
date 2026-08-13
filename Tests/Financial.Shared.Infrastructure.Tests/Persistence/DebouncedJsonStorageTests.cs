using System.Diagnostics;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using Financial.Shared.Infrastructure.Tests.TestHelpers;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

public class DebouncedJsonStorageTests
{
    [Fact]
    public async Task WriteAsync_ReturnsImmediately_AndStatusBecomesPending()
    {
        var inner = new ControllableJsonStorage();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromSeconds(10));

        var writeTask = storage.WriteAsync("{\"a\":1}");

        writeTask.IsCompleted.Should().BeTrue();
        await writeTask;
        storage.GetStatus().State.Should().Be(SyncState.Pending);
        inner.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task AfterDebounceWindowElapses_LatestJsonIsUploaded()
    {
        var inner = new ControllableJsonStorage();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(50));

        await storage.WriteAsync("{\"a\":1}");

        await WaitForAsync(() => inner.WrittenJson.Count == 1);

        inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
    }

    [Fact]
    public async Task WriteDuringDebounceWindow_ResetsWait_OnlyLatestJsonUploaded()
    {
        var inner = new ControllableJsonStorage();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(150));

        await storage.WriteAsync("{\"a\":1}");
        await Task.Delay(50);
        await storage.WriteAsync("{\"a\":2}");

        await WaitForAsync(() => inner.WrittenJson.Count >= 1);
        await Task.Delay(100);

        inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":2}");
    }

    [Fact]
    public async Task WriteDuringInFlightSave_StartsFollowUpCycleWithoutBlockingTheWrite()
    {
        var inner = new ControllableJsonStorage();
        inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(30));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        var stopwatch = Stopwatch.StartNew();
        var secondWrite = storage.WriteAsync("{\"a\":2}");
        stopwatch.Stop();

        secondWrite.IsCompleted.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);

        inner.Release();

        await WaitForAsync(() => inner.WrittenJson.Count == 2);
        inner.WrittenJson[0].Should().Be("{\"a\":1}");
        inner.WrittenJson[1].Should().Be("{\"a\":2}");
    }

    [Fact]
    public async Task RetriesExhausted_StatusBecomesFailed_LastSuccessfulSaveUtcPreserved()
    {
        var inner = new ControllableJsonStorage();
        var fixedTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero));
        var storage = new DebouncedJsonStorage(
            inner, TimeSpan.FromMilliseconds(20), fixedTime, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);
        var successTimestamp = storage.GetStatus().LastSuccessfulSaveUtc;
        successTimestamp.Should().NotBeNull();

        inner.FailNextWrites(1);
        await storage.WriteAsync("{\"a\":2}");

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);

        var status = storage.GetStatus();
        status.LastError.Should().Contain("Simulated transient storage failure");
        status.LastSuccessfulSaveUtc.Should().Be(successTimestamp);
    }

    [Fact]
    public async Task SaveFailure_DoesNotAutoStartFollowUpCycle()
    {
        var inner = new ControllableJsonStorage();
        inner.HoldWritesUntilReleased();
        inner.FailNextWrites(1);
        var storage = new DebouncedJsonStorage(
            inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 0, flushTimeout: TimeSpan.FromSeconds(8));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        await storage.WriteAsync("{\"a\":2}");

        inner.Release();

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Failed);
        await Task.Delay(200);

        inner.WrittenJson.Should().BeEmpty();
        storage.GetStatus().State.Should().Be(SyncState.Failed);
    }

    [Fact]
    public async Task SuccessfulSave_StatusBecomesIdle_LastSuccessfulSaveUtcUpdates()
    {
        var inner = new ControllableJsonStorage();
        var fixedTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(20), fixedTime);

        await storage.WriteAsync("{\"a\":1}");

        await WaitForAsync(() => storage.GetStatus().State == SyncState.Idle);

        storage.GetStatus().LastSuccessfulSaveUtc.Should().Be(fixedTime.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task SuccessfulSave_WhenStillDirtyFromANewerWrite_StatusBecomesPendingNotIdle()
    {
        var inner = new ControllableJsonStorage();
        inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(300));

        await storage.WriteAsync("{\"a\":1}");
        await WaitForAsync(() => storage.GetStatus().State == SyncState.Saving);

        await storage.WriteAsync("{\"a\":2}");

        inner.Release();

        await WaitForAsync(() => inner.WrittenJson.Count == 1);

        storage.GetStatus().State.Should().Be(SyncState.Pending);
    }

    [Fact]
    public async Task FlushAsync_OnDirtyInstance_SavesImmediatelyWithoutWaitingForDebounce()
    {
        var inner = new ControllableJsonStorage();
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromSeconds(30));

        await storage.WriteAsync("{\"a\":1}");
        inner.WrittenJson.Should().BeEmpty();

        await storage.FlushAsync();

        inner.WrittenJson.Should().ContainSingle().Which.Should().Be("{\"a\":1}");
        storage.GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task FlushAsync_WhenSaveExceedsTimeout_ReturnsWithoutWaitingFurther()
    {
        var inner = new ControllableJsonStorage();
        inner.HoldWritesUntilReleased();
        var storage = new DebouncedJsonStorage(
            inner, TimeSpan.FromMilliseconds(20), null, maxRetries: 5, flushTimeout: TimeSpan.FromMilliseconds(100));

        await storage.WriteAsync("{\"a\":1}");

        var stopwatch = Stopwatch.StartNew();
        await storage.FlushAsync();
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        inner.WrittenJson.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsync_PassesThroughToWrappedStorage()
    {
        var inner = new ControllableJsonStorage { ReadResult = "{\"x\":true}" };
        var storage = new DebouncedJsonStorage(inner, TimeSpan.FromMilliseconds(50));

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
