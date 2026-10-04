using Financial.CashFlow.Infrastructure.Repositories;
using Financial.TestUtilities;

namespace Financial.CashFlow.Infrastructure.Tests;

public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(CashFlowJsonRepository).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(CashFlowJsonRepository).Assembly, typeName, constructorIndex, parameterName);
}
