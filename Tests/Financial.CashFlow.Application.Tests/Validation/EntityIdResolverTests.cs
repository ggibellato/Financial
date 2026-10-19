using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

[Trait("Category", "Unit")]
public class EntityIdResolverTests
{
    private static readonly Account[] Banks =
    [
        TestAccounts.Bank("Barclays", roundUpEnabled: false),
        TestAccounts.Bank("Trading212", roundUpEnabled: true),
        TestAccounts.Bank("Chase", roundUpEnabled: true)
    ];

    [Fact]
    public void TryResolve_KnownId_ReturnsTrueAndTheEntity()
    {
        var target = Banks[1];

        var result = EntityIdResolver.TryResolve(target.Id, Banks, b => b.Id, out var bank);

        result.Should().BeTrue();
        bank.Should().NotBeNull();
        bank!.Name.Should().Be("Trading212");
        bank.RoundUp!.Enabled.Should().BeTrue();
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
