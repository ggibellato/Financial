using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Shared.Abstractions.Tests.Validation;

[Trait("Category", "Unit")]
public class EnumParserTests
{
    [Fact]
    public void TryParseEnum_LowercaseName_ReturnsTrueWithTheMatchingValue()
    {
        var parsed = EnumParser.TryParseEnum<Currency>("gbp", out var currency);

        using var _ = new AssertionScope();
        parsed.Should().BeTrue();
        currency.Should().Be(Currency.GBP);
    }

    [Fact]
    public void TryParseEnum_UnknownName_ReturnsFalse()
    {
        EnumParser.TryParseEnum<Currency>("XYZ", out _).Should().BeFalse();
    }
}
