using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Financial.Shared.Infrastructure.Observability;

/// <summary>
/// Wraps an Application-layer service interface so every call produces a span named
/// "{BoundedContext}.{InterfaceName}.{MethodName}", without the wrapped Application project ever
/// referencing ITelemetryTracer or any observability type (research.md Decision D3). Built on the
/// BCL's System.Reflection.DispatchProxy — no third-party proxy/decoration library.
/// </summary>
public class TracingDispatchProxy<TInterface> : DispatchProxy
    where TInterface : class
{
    private TInterface _target = default!;
    private ITelemetryTracer _tracer = default!;
    private string _boundedContext = default!;

    public static TInterface Create(TInterface target, ITelemetryTracer tracer, string boundedContext)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(tracer);
        ArgumentNullException.ThrowIfNull(boundedContext);

        var proxyAsInterface = Create<TInterface, TracingDispatchProxy<TInterface>>()!;
        var proxy = (TracingDispatchProxy<TInterface>)(object)proxyAsInterface;
        proxy._target = target;
        proxy._tracer = tracer;
        proxy._boundedContext = boundedContext;
        return proxyAsInterface;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        var spanName = $"{_boundedContext}.{InterfaceDisplayName}.{targetMethod.Name}";
        var span = _tracer.StartSpan(spanName);
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, _boundedContext);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, spanName);

        object? result;
        try
        {
            result = targetMethod.Invoke(_target, args);
        }
        catch (Exception ex)
        {
            // MethodInfo.Invoke wraps exceptions thrown by the target in TargetInvocationException;
            // unwrap it so callers (e.g. exception-mapping middleware) still see the original type.
            // Any other exception (including a TargetInvocationException with no InnerException,
            // which reflection can still produce) is completed/rethrown as-is — the span must never
            // leak uncompleted just because the failure didn't take the expected shape.
            var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
            CompleteSpan(span, actual, canceled: false);
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw; // unreachable — ExceptionDispatchInfo.Throw() never returns
        }

        if (result is Task task)
        {
            task.ContinueWith(
                completed => CompleteSpan(
                    span,
                    completed.IsFaulted ? completed.Exception?.GetBaseException() : null,
                    canceled: completed.IsCanceled),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return result;
        }

        CompleteSpan(span, exception: null, canceled: false);
        return result;
    }

    private static void CompleteSpan(ITelemetrySpan span, Exception? exception, bool canceled)
    {
        span.SetAttribute(
            TelemetryAttributeKeys.OperationResult,
            canceled ? TelemetryOperationResults.Canceled : exception is not null
                ? TelemetryOperationResults.Failed
                : TelemetryOperationResults.Success);

        if (exception is not null)
        {
            span.RecordException(exception);
        }

        span.Dispose();
    }

    private static string InterfaceDisplayName
    {
        get
        {
            var name = typeof(TInterface).Name;
            return name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]) ? name[1..] : name;
        }
    }
}
