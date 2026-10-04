using Financial.Presentation.App.ViewModels;
using Financial.TestUtilities;

namespace Financial.Presentation.Tests;

public class ConstructorGuardTests
{
    private static readonly Dictionary<string, string> Allowed = new()
    {
        ["AssetPriceFetchViewModel.options"] = "null options deliberately means an empty portfolio list"
    };

    private static bool IsViewModel(Type type) => type.Namespace?.StartsWith("Financial.Presentation.App.ViewModels") == true;

    public static IEnumerable<object[]> Cases() => ConstructorGuardAssertions.Cases(typeof(AdminEntityPlaceholderViewModel).Assembly, IsViewModel, injectedServicesOnly: true);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Constructor_RejectsNullDependency(string typeName, int constructorIndex, string parameterName) =>
        ConstructorGuardAssertions.AssertRejectsNull(typeof(AdminEntityPlaceholderViewModel).Assembly, typeName, constructorIndex, parameterName, IsViewModel, Allowed);
}
