using Financial.Shared.Infrastructure.Observability;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Observability;

public class TracingDispatchProxyTests
{
    [Fact]
    public void Invoke_SyncMethodSucceeds_StartsSpanWithExpectedNameAndSuccessResult()
    {
        var tracer = new RecordingTelemetryTracer();
        var proxy = TracingDispatchProxy<ISampleService>.Create(new SampleService(), tracer, "CashFlow");

        var result = proxy.GetValue();

        result.Should().Be(42);
        var span = tracer.CompletedSpans.Should().ContainSingle().Subject;
        span.Name.Should().Be("CashFlow.SampleService.GetValue");
        span.Attributes["operation.result"].Should().Be("success");
        span.Exception.Should().BeNull();
    }

    [Fact]
    public void Invoke_SyncMethodThrows_RecordsExceptionAndRethrowsOriginalType()
    {
        var tracer = new RecordingTelemetryTracer();
        var proxy = TracingDispatchProxy<ISampleService>.Create(new SampleService(), tracer, "CashFlow");

        var act = () => proxy.ThrowSync();

        act.Should().Throw<InvalidOperationException>().WithMessage("sync failure");
        var span = tracer.CompletedSpans.Should().ContainSingle().Subject;
        span.Attributes["operation.result"].Should().Be("failed");
        span.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Invoke_AsyncTaskMethodSucceeds_CompletesSpanAfterTaskFinishes()
    {
        var tracer = new RecordingTelemetryTracer();
        var proxy = TracingDispatchProxy<ISampleService>.Create(new SampleService(), tracer, "Investment");

        await proxy.DoWorkAsync();

        var span = tracer.CompletedSpans.Should().ContainSingle().Subject;
        span.Name.Should().Be("Investment.SampleService.DoWorkAsync");
        span.Attributes["operation.result"].Should().Be("success");
    }

    [Fact]
    public async Task Invoke_AsyncTaskOfTMethodThrows_RecordsExceptionAndRethrowsToAwaiter()
    {
        var tracer = new RecordingTelemetryTracer();
        var proxy = TracingDispatchProxy<ISampleService>.Create(new SampleService(), tracer, "CashFlow");

        var act = async () => await proxy.ThrowAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("async failure");
        var span = tracer.CompletedSpans.Should().ContainSingle().Subject;
        span.Attributes["operation.result"].Should().Be("failed");
        span.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Invoke_AsyncTaskMethodCanceled_CompletesSpanAsCanceledWithoutRecordingException()
    {
        var tracer = new RecordingTelemetryTracer();
        var proxy = TracingDispatchProxy<ISampleService>.Create(new SampleService(), tracer, "CashFlow");

        var act = async () => await proxy.CancelAsync();

        await act.Should().ThrowAsync<TaskCanceledException>();
        var span = tracer.CompletedSpans.Should().ContainSingle().Subject;
        span.Attributes["operation.result"].Should().Be("canceled");
        span.Exception.Should().BeNull();
    }

    private interface ISampleService
    {
        int GetValue();
        void ThrowSync();
        Task DoWorkAsync();
        Task<int> ThrowAsync();
        Task CancelAsync();
    }

    private sealed class SampleService : ISampleService
    {
        public int GetValue() => 42;

        public void ThrowSync() => throw new InvalidOperationException("sync failure");

        public Task DoWorkAsync() => Task.CompletedTask;

        public async Task<int> ThrowAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("async failure");
        }

        public Task CancelAsync()
        {
            var source = new TaskCompletionSource();
            source.SetCanceled();
            return source.Task;
        }
    }

    private sealed class RecordingTelemetryTracer : ITelemetryTracer
    {
        public List<RecordedSpan> CompletedSpans { get; } = [];

        public ITelemetrySpan StartSpan(string name) => new RecordingTelemetrySpan(name, this);

        private sealed class RecordingTelemetrySpan(string name, RecordingTelemetryTracer owner) : ITelemetrySpan
        {
            private readonly Dictionary<string, string> _attributes = [];
            private Exception? _exception;

            public void SetAttribute(string key, string value) => _attributes[key] = value;

            public void RecordException(Exception exception) => _exception = exception;

            public void Dispose() =>
                owner.CompletedSpans.Add(new RecordedSpan(name, new Dictionary<string, string>(_attributes), _exception));
        }
    }

    private sealed record RecordedSpan(string Name, Dictionary<string, string> Attributes, Exception? Exception);
}
