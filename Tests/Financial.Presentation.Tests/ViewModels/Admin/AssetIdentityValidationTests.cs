using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class AssetIdentityValidationTests
{
    [Fact]
    public void ValidateIsin_Blank_ReturnsNull()
    {
        AssetIdentityValidation.ValidateIsin(string.Empty).Should().BeNull();
    }

    [Fact]
    public void ValidateIsin_Valid_ReturnsNull()
    {
        AssetIdentityValidation.ValidateIsin("US0378331005").Should().BeNull();
    }

    [Fact]
    public void ValidateIsin_Invalid_ReturnsMessage()
    {
        AssetIdentityValidation.ValidateIsin("NOT-AN-ISIN").Should()
            .Be("ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).");
    }
}
