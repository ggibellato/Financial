using Financial.CashFlow.Application.Validation;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

[Trait("Category", "Unit")]
public class DescriptionValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EnsureWithinLimit_NoDescription_IsAccepted(string? description)
    {
        var act = () => DescriptionValidator.EnsureWithinLimit(description);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureWithinLimit_ExactlyTwoHundredCharacters_IsAccepted()
    {
        var act = () => DescriptionValidator.EnsureWithinLimit(new string('a', 200));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureWithinLimit_TwoHundredAndOneCharacters_IsRejectedNamingTheLimit()
    {
        var act = () => DescriptionValidator.EnsureWithinLimit(new string('a', 201));

        act.Should().Throw<ArgumentException>().WithMessage("Description must not exceed 200 characters.");
    }

    [Fact]
    public void EnsureWithinLimit_GivenLimit_IsAppliedInsteadOfTheDefault()
    {
        var act = () => DescriptionValidator.EnsureWithinLimit("abcdef", maxLength: 5);

        act.Should().Throw<ArgumentException>().WithMessage("Description must not exceed 5 characters.");
    }
}
