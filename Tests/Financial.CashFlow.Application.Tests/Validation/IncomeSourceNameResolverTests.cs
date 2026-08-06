using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class IncomeSourceNameResolverTests
{
    private static readonly IncomeSource[] Sources =
    [
        IncomeSource.Create("Gleison", IncomeGroup.Salary),
        IncomeSource.Create("Ariana", IncomeGroup.Salary),
        IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false),
        IncomeSource.Create("DividendoJuros", IncomeGroup.DividendoJuros)
    ];

    [Fact]
    public void TryResolve_ExactCaseName_ReturnsTrueAndTheSource()
    {
        var result = IncomeSourceNameResolver.TryResolve("Gleison", Sources, out var source);

        result.Should().BeTrue();
        source.Should().NotBeNull();
        source!.Name.Should().Be("Gleison");
        source.Group.Should().Be(IncomeGroup.Salary);
    }

    [Fact]
    public void TryResolve_DifferentCasing_ResolvesCaseInsensitively()
    {
        var result = IncomeSourceNameResolver.TryResolve("aRIANA", Sources, out var source);

        result.Should().BeTrue();
        source!.Name.Should().Be("Ariana");
    }

    [Fact]
    public void TryResolve_UnknownName_ReturnsFalse()
    {
        var result = IncomeSourceNameResolver.TryResolve("NotASource", Sources, out var source);

        result.Should().BeFalse();
        source.Should().BeNull();
    }

    [Fact]
    public void TryResolve_NullValue_ReturnsFalse()
    {
        var result = IncomeSourceNameResolver.TryResolve(null, Sources, out var source);

        result.Should().BeFalse();
        source.Should().BeNull();
    }

    [Fact]
    public void TryResolve_BlankValue_ReturnsFalse()
    {
        var result = IncomeSourceNameResolver.TryResolve("   ", Sources, out var source);

        result.Should().BeFalse();
        source.Should().BeNull();
    }

    [Fact]
    public void TryResolve_InactiveSource_StillResolves()
    {
        var result = IncomeSourceNameResolver.TryResolve("Lottery", Sources, out var source);

        result.Should().BeTrue();
        source!.IsActive.Should().BeFalse();
    }
}
