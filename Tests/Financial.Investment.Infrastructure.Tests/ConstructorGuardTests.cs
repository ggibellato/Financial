using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;

namespace Financial.Investment.Infrastructure.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(InvestmentJsonRepository).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(InvestmentJsonRepository).Assembly, typeName, constructorIndex, parameterName);
}
