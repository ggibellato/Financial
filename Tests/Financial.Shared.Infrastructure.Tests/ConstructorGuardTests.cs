using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;

namespace Financial.Shared.Infrastructure.Tests;

public class ConstructorGuardTests
{
    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(JsonStorageFactory).Assembly);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(JsonStorageFactory).Assembly, typeName, constructorIndex, parameterName);
}
