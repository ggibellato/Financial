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
    public void TryResolve_KnownId_ReturnsTrueAndTheBank()
    {
        var target = Banks[1];

        var result = BankNameResolver.TryResolve(target.Id, Banks, out var bank);

        result.Should().BeTrue();
        bank.Should().NotBeNull();
        bank!.Name.Should().Be("Trading212");
        bank.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var result = BankNameResolver.TryResolve(Guid.NewGuid(), Banks, out var bank);

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
}
