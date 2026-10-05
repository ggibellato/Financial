using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace Financial.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class UseCultureAttribute : BeforeAfterTestAttribute
{
    private readonly CultureInfo _culture;
    private CultureInfo? _originalCulture;
    private CultureInfo? _originalUiCulture;

    public UseCultureAttribute(string culture) => _culture = CultureInfo.GetCultureInfo(culture);

    public override void Before(MethodInfo methodUnderTest)
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _culture;
    }

    public override void After(MethodInfo methodUnderTest)
    {
        CultureInfo.CurrentCulture = _originalCulture!;
        CultureInfo.CurrentUICulture = _originalUiCulture!;
    }
}
