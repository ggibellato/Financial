using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(TransferPairRules).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(TransferPairRules).Assembly, typeName, constructorIndex, parameterName);
}
