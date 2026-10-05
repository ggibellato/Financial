using System.Globalization;
using System.Runtime.CompilerServices;

namespace Financial.Tests;

internal static class TestCultureInitializer
{
    [ModuleInitializer]
    internal static void Apply()
    {
        var name = Environment.GetEnvironmentVariable("TEST_CULTURE");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var culture = CultureInfo.GetCultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
