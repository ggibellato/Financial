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
    public void TryResolve_KnownId_ReturnsTrueAndTheSource()
    {
        var target = Sources[0];

        var result = IncomeSourceNameResolver.TryResolve(target.Id, Sources, out var source);

        result.Should().BeTrue();
        source.Should().NotBeNull();
        source!.Name.Should().Be("Gleison");
        source.Group.Should().Be(IncomeGroup.Salary);
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var result = IncomeSourceNameResolver.TryResolve(Guid.NewGuid(), Sources, out var source);

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
    public void TryResolve_InactiveSource_StillResolves()
    {
        var target = Sources[2];

        var result = IncomeSourceNameResolver.TryResolve(target.Id, Sources, out var source);

        result.Should().BeTrue();
        source!.IsActive.Should().BeFalse();
    }
}
