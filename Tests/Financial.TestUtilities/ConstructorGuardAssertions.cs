using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Financial.TestUtilities;

/// <summary>Reflection-driven replacement for one hand-written "constructor with null X throws" test
/// per dependency: every public constructor of every public class in an assembly is called once per
/// non-nullable reference parameter with null in that slot and sensible stand-ins elsewhere, and must
/// throw <see cref="ArgumentNullException"/> naming that parameter.</summary>
public static class ConstructorGuardAssertions
{
    public static IEnumerable<object[]> Cases(Assembly assembly, Func<Type, bool>? include = null, bool injectedServicesOnly = false) =>
        GuardedTypes(assembly, include)
            .SelectMany(type => Constructors(type).Select((ctor, index) => (type, ctor, index)))
            .SelectMany(entry => NullableSlots(entry.ctor, injectedServicesOnly).Select(parameter =>
                new object[] { entry.type.Name, entry.index, parameter.Name! }));

    public static void AssertRejectsNull(
        Assembly assembly,
        string typeName,
        int constructorIndex,
        string parameterName,
        Func<Type, bool>? include = null,
        IReadOnlyDictionary<string, string>? allowed = null)
    {
        var type = GuardedTypes(assembly, include).Single(t => t.Name == typeName);
        var constructor = Constructors(type)[constructorIndex];
        var parameters = constructor.GetParameters();
        var slot = Array.FindIndex(parameters, p => p.Name == parameterName);

        if (allowed is not null && allowed.ContainsKey($"{typeName}.{parameterName}"))
        {
            return;
        }

        var arguments = parameters.Select(p => CreateArgument(p.ParameterType, 0)).ToArray();
        arguments[slot] = null;

        Exception? thrown = null;
        try
        {
            constructor.Invoke(arguments);
        }
        catch (TargetInvocationException ex)
        {
            thrown = ex.InnerException;
        }

        if (thrown is not ArgumentNullException guard || guard.ParamName != parameterName)
        {
            throw new InvalidOperationException(
                $"{type.Name}({parameterName}): expected ArgumentNullException for parameter '{parameterName}' but got " +
                (thrown is null ? "no exception - add a guard or an allowlist entry with a reason" : $"{thrown.GetType().Name}: {thrown.Message}"));
        }
    }

    private static ConstructorInfo[] Constructors(Type type) =>
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(constructor => constructor.IsPublic || constructor.IsAssembly)
            .ToArray();

    private static IEnumerable<Type> GuardedTypes(Assembly assembly, Func<Type, bool>? include) =>
        assembly.GetTypes().Where(type =>
            (include is null || include(type))
            && type.IsClass
            && !type.IsNested
            && !Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
            && !type.IsAbstract
            && !type.IsGenericTypeDefinition
            && !typeof(Exception).IsAssignableFrom(type)
            && !typeof(Attribute).IsAssignableFrom(type)
            && !typeof(Delegate).IsAssignableFrom(type)
            && type.GetMethod("<Clone>$") is null);

    private static IEnumerable<ParameterInfo> NullableSlots(ConstructorInfo constructor, bool injectedServicesOnly)
    {
        var context = new NullabilityInfoContext();
        return constructor.GetParameters().Where(parameter =>
            !parameter.ParameterType.IsValueType
            && !parameter.ParameterType.IsArray
            && (!injectedServicesOnly || IsInjectedCollaborator(parameter.ParameterType))
            && !parameter.HasDefaultValue
            && context.Create(parameter).WriteState == NullabilityState.NotNull);
    }

    private static bool IsInjectedCollaborator(Type type) =>
        type.IsInterface && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

    private static object? CreateArgument(Type type, int depth)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type == typeof(string))
        {
            return "x";
        }

        if (type == typeof(TimeProvider))
        {
            return TimeProvider.System;
        }

        if (type == typeof(HttpMessageHandler))
        {
            return new HttpClientHandler();
        }

        if (type == typeof(TimeZoneInfo))
        {
            return TimeZoneInfo.Utc;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return CreateDelegate(type);
        }

        if (type.IsInterface)
        {
            return CreateInterface(type, depth);
        }

        if (!type.IsAbstract && depth < 4)
        {
            return CreateConcrete(type, depth);
        }

        throw new InvalidOperationException($"No argument factory for {type.FullName}; add one to ConstructorGuardAssertions.");
    }

    private static object? CreateInterface(Type type, int depth)
    {
        if (type == typeof(ILogger))
        {
            return NullLogger.Instance;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var argument = type.GetGenericArguments()[0];

            if (definition == typeof(ILogger<>))
            {
                return typeof(NullLogger<>).MakeGenericType(argument).GetField("Instance")!.GetValue(null);
            }

            if (definition == typeof(IOptions<>))
            {
                return typeof(Options).GetMethod(nameof(Options.Create))!.MakeGenericMethod(argument)
                    .Invoke(null, [CreateArgument(argument, depth + 1)]);
            }

            var empty = Array.CreateInstance(argument, 0);
            if (type.IsInstanceOfType(empty))
            {
                return empty;
            }
        }

        return typeof(DispatchProxy).GetMethods()
            .Single(m => m.Name == nameof(DispatchProxy.Create) && m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(type, typeof(DoNothingProxy))
            .Invoke(null, null);
    }

    private static object CreateConcrete(Type type, int depth)
    {
        var constructor = Constructors(type).OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        if (constructor is null)
        {
            var anyVisibility = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.GetConstructor(anyVisibility, Type.EmptyTypes) is { } parameterless)
            {
                return parameterless.Invoke(null);
            }

            throw new InvalidOperationException($"No argument factory for {type.FullName}; add one to ConstructorGuardAssertions.");
        }

        return constructor.Invoke(constructor.GetParameters().Select(p => CreateArgument(p.ParameterType, depth + 1)).ToArray());
    }

    private static Delegate CreateDelegate(Type type)
    {
        var invoke = type.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        var body = invoke.ReturnType == typeof(void)
            ? (Expression)Expression.Empty()
            : Expression.Constant(DoNothingProxy.DefaultFor(invoke.ReturnType), invoke.ReturnType);
        return Expression.Lambda(type, body, parameters).Compile();
    }

    public class DoNothingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            DefaultFor(targetMethod!.ReturnType);

        internal static object? DefaultFor(Type type)
        {
            if (type == typeof(void))
            {
                return null;
            }

            if (type == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(type.GetGenericArguments()[0])
                    .Invoke(null, [DefaultFor(type.GetGenericArguments()[0])]);
            }

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
