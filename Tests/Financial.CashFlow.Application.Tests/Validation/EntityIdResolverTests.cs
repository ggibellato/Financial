using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class EntityIdResolverTests
{
    private static readonly Bank[] Banks =
    [
        Bank.Create("Barclays", roundUpEnabled: false),
        Bank.Create("Trading212", roundUpEnabled: true),
        Bank.Create("Chase", roundUpEnabled: true)
    ];

    [Fact]
    public void TryResolve_KnownId_ReturnsTrueAndTheEntity()
    {
        var target = Banks[1];

        var result = EntityIdResolver.TryResolve(target.Id, Banks, b => b.Id, out var bank);

        result.Should().BeTrue();
        bank.Should().NotBeNull();
        bank!.Name.Should().Be("Trading212");
        bank.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var result = EntityIdResolver.TryResolve(Guid.NewGuid(), Banks, b => b.Id, out var bank);

        result.Should().BeFalse();
        bank.Should().BeNull();
    }

    [Fact]
    public void TryResolve_NullValue_ReturnsFalse()
    {
        var result = EntityIdResolver.TryResolve(null, Banks, b => b.Id, out var bank);

        result.Should().BeFalse();
        bank.Should().BeNull();
    }
}
