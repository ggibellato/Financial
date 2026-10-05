using System.Reflection;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Shared.Abstractions.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    private static readonly Assembly Target = typeof(InMemoryCachedExchangeRateProvider).Assembly;

    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(Target);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(Target, typeName, constructorIndex, parameterName);
}
