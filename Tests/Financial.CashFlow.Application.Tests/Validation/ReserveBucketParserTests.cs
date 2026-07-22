using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class ReserveBucketParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = ReserveBucketParser.TryParse("HouseTreats", out var bucket);

        result.Should().BeTrue();
        bucket.Should().Be(ReserveBucket.HouseTreats);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = ReserveBucketParser.TryParse("NotABucket", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = ReserveBucketParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
