using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CreditCardNameResolverTests
{
    private static readonly CreditCard[] Cards =
    [
        CreditCard.Create("BarclaysPlatinumVisa8003"),
        CreditCard.Create("ChaseMaster4023"),
        CreditCard.Create("BaAmex", isActive: false)
    ];

    [Fact]
    public void TryResolve_KnownId_ReturnsTrueAndTheCard()
    {
        var target = Cards[1];

        var result = CreditCardNameResolver.TryResolve(target.Id, Cards, out var card);

        result.Should().BeTrue();
        card.Should().NotBeNull();
        card!.Name.Should().Be("ChaseMaster4023");
        card.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TryResolve_KnownId_ResolvesRegardlessOfActiveFlag()
    {
        var target = Cards[2];

        var result = CreditCardNameResolver.TryResolve(target.Id, Cards, out var card);

        result.Should().BeTrue();
        card!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var result = CreditCardNameResolver.TryResolve(Guid.NewGuid(), Cards, out var card);

        result.Should().BeFalse();
        card.Should().BeNull();
    }

    [Fact]
    public void TryResolve_NullValue_ReturnsFalse()
    {
        var result = CreditCardNameResolver.TryResolve(null, Cards, out var card);

        result.Should().BeFalse();
        card.Should().BeNull();
    }
}
