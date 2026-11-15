using Financial.Shared.Abstractions.Observability;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Shared.Abstractions.Tests.Observability;

[Trait("Category", "Unit")]
public class TelemetryExtensionsTests
{
    private readonly RecordingTelemetryTracer _tracer = new();

    [Fact]
    public void StartServiceSpan_NamesTheSpanByContextServiceAndOperation_AndSetsTheStandardAttributes()
    {
        _tracer.StartServiceSpan("CashFlow", "ExpenseService", "AddExpense", "Expense");

        var span = _tracer.Spans.Single();

        using var _ = new AssertionScope();
        span.Name.Should().Be("CashFlow.ExpenseService.AddExpense");
        span.Attributes.Should().Equal(new Dictionary<string, string>
        {
            [TelemetryAttributeKeys.BoundedContext] = "CashFlow",
            [TelemetryAttributeKeys.EntityType] = "Expense",
            [TelemetryAttributeKeys.OperationName] = "AddExpense"
        });
    }

    [Fact]
    public void MarkSuccess_SetsTheSuccessResult()
    {
        _tracer.StartSpan("Operation").MarkSuccess();

        var span = _tracer.Spans.Single();
        using var _ = new AssertionScope();
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        span.RecordedException.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_SetsTheFailedResultAndRecordsTheException()
    {
        var exception = new InvalidOperationException("failure");

        _tracer.StartSpan("Operation").MarkFailed(exception);

        var span = _tracer.Spans.Single();
        using var _ = new AssertionScope();
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().BeSameAs(exception);
    }
}
