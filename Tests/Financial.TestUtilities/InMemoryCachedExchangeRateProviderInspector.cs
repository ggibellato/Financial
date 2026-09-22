using System.Reflection;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class InMemoryCachedExchangeRateProviderInspector
{
    public static IExchangeRateProvider InvokeInnerFactory(IExchangeRateProvider cachedProvider)
    {
        var field = typeof(InMemoryCachedExchangeRateProvider).GetField(
            "_innerFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var factory = (Func<IExchangeRateProvider>)field.GetValue(cachedProvider)!;
        return factory();
    }
}
