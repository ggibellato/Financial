using Financial.Investment.Application.Services;
using Financial.TestUtilities;

namespace Financial.Investment.Application.Tests;

public class ConstructorGuardTests
{
    private static readonly Dictionary<string, string> Allowed = new()
    {
        ["CurrencyConversionContext.exchangeRateProvider"] = "internal helper constructed only by services that guard the provider themselves"
    };

    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(AllocationBreakdownService).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(AllocationBreakdownService).Assembly, typeName, constructorIndex, parameterName, allowed: Allowed);
}
