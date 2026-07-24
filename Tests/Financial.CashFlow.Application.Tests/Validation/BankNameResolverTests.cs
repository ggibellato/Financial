using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class BankNameResolverTests
{
    private static readonly Bank[] Banks =
    [
        Bank.Create("Barclays", roundUpEnabled: false),
        Bank.Create("Trading212", roundUpEnabled: true),
        Bank.Create("Chase", roundUpEnabled: true)
    ];

    [Fact]
    public void TryResolve_ExactCaseName_ReturnsTrueAndTheBank()
    {
        var result = BankNameResolver.TryResolve("Trading212", Banks, out var bank);

        result.Should().BeTrue();
        bank.Should().NotBeNull();
        bank!.Name.Should().Be("Trading212");
        bank.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public void TryResolve_DifferentCasing_ResolvesCaseInsensitively()
    {
        var result = BankNameResolver.TryResolve("bARCLAYS", Banks, out var bank);

        result.Should().BeTrue();
        bank!.Name.Should().Be("Barclays");
    }

    [Fact]
    public void TryResolve_UnknownName_ReturnsFalse()
    {
        var result = BankNameResolver.TryResolve("NotABank", Banks, out var bank);

        result.Should().BeFalse();
        bank.Should().BeNull();
    }

    [Fact]
    public void TryResolve_NullValue_ReturnsFalse()
    {
        var result = BankNameResolver.TryResolve(null, Banks, out var bank);

        result.Should().BeFalse();
        bank.Should().BeNull();
    }

    [Fact]
    public void TryResolve_BlankValue_ReturnsFalse()
    {
        var result = BankNameResolver.TryResolve("   ", Banks, out var bank);

        result.Should().BeFalse();
        bank.Should().BeNull();
    }
}
