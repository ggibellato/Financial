using Financial.Shared.Abstractions.Observability;
using FluentAssertions;

namespace Financial.TestUtilities;

public static class TelemetryAssertions
{
    public static void ShouldHaveFailedSpan<TException>(this RecordingTelemetryTracer tracer, string spanName)
        where TException : Exception
    {
        var span = tracer.Spans.Should().ContainSingle(s => s.Name == spanName).Which;

        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().BeOfType<TException>();
        span.Disposed.Should().BeTrue();
    }

    public static RecordingTelemetryTracer.RecordedSpan ShouldHaveSucceededSpan(this RecordingTelemetryTracer tracer, string spanName)
    {
        var span = tracer.Spans.Should().ContainSingle(s => s.Name == spanName).Which;

        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        span.RecordedException.Should().BeNull();
        span.Disposed.Should().BeTrue();
        return span;
    }
}
