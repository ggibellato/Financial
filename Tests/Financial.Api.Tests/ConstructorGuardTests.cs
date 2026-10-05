using Financial.Api.Controllers;
using Financial.TestUtilities;

namespace Financial.Api.Tests;

[Trait("Category", "Unit")]
public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(AssetsController).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(AssetsController).Assembly, typeName, constructorIndex, parameterName);
}
