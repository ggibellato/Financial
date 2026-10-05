using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;

namespace Financial.Shared.Abstractions.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(InMemoryCachedExchangeRateProvider).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(InMemoryCachedExchangeRateProvider).Assembly, typeName, constructorIndex, parameterName);
}
