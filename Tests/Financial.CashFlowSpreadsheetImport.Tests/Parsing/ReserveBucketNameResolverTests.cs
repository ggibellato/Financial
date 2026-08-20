using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class ReserveBucketNameResolverTests
{
    private static readonly ReserveBucket[] Buckets =
    [
        ReserveBucket.Create("Investimento", 33.33m),
        ReserveBucket.Create("HouseTreats", 33.33m),
        ReserveBucket.Create("Ariana", 16.67m)
    ];

    [Fact]
    public void TryResolve_KnownName_ReturnsTrueAndTheBucket()
    {
        var result = ReserveBucketNameResolver.TryResolve("HouseTreats", Buckets, out var bucket);

        result.Should().BeTrue();
        bucket.Should().NotBeNull();
        bucket!.Name.Should().Be("HouseTreats");
        bucket.SplitPercentage.Should().Be(33.33m);
    }

    [Fact]
    public void TryResolve_KnownNameDifferentCasing_ReturnsTrueAndTheBucket()
    {
        var result = ReserveBucketNameResolver.TryResolve("housetreats", Buckets, out var bucket);

        result.Should().BeTrue();
        bucket!.Name.Should().Be("HouseTreats");
    }

    [Fact]
    public void TryResolve_UnknownName_ReturnsFalse()
    {
        var result = ReserveBucketNameResolver.TryResolve("NotABucket", Buckets, out var bucket);

        result.Should().BeFalse();
        bucket.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_BlankName_ReturnsFalse(string? name)
    {
        var result = ReserveBucketNameResolver.TryResolve(name, Buckets, out var bucket);

        result.Should().BeFalse();
        bucket.Should().BeNull();
    }
}
