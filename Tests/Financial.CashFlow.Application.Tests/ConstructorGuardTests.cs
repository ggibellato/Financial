using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;

namespace Financial.CashFlow.Application.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(BankService).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(BankService).Assembly, typeName, constructorIndex, parameterName);
}
