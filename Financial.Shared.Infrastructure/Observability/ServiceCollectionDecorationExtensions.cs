using Microsoft.Extensions.DependencyInjection;

namespace Financial.Shared.Infrastructure.Observability;

public static class ServiceCollectionDecorationExtensions
{
    /// <summary>
    /// Re-registers TInterface's existing implementation wrapped in a TracingDispatchProxy, so
    /// every call to it produces a span. The wrapped Application-layer implementation is never
    /// modified and never references ITelemetryTracer directly (research.md Decision D3).
    /// </summary>
    public static IServiceCollection Decorate<TInterface>(this IServiceCollection services, string boundedContext)
        where TInterface : class
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TInterface))
            ?? throw new InvalidOperationException(
                $"No registration found for {typeof(TInterface).Name} to decorate. " +
                "Call Decorate<TInterface>() after the real implementation has been registered.");

        services.Remove(descriptor);

        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp =>
            {
                var inner = (TInterface)CreateInstance(descriptor, sp);
                var tracer = sp.GetRequiredService<ITelemetryTracer>();
                return TracingDispatchProxy<TInterface>.Create(inner, tracer, boundedContext);
            },
            descriptor.Lifetime));

        return services;
    }

    private static object CreateInstance(ServiceDescriptor descriptor, IServiceProvider serviceProvider)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory(serviceProvider);
        }

        return ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType!);
    }
}
