using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CategoryNameResolverTests
{
    private static readonly Category[] Categories =
    [
        Category.Create("Mercado"),
        Category.Create("Investimento", isInvestment: true),
        Category.Create("RetiredCategory", isActive: false)
    ];

    [Fact]
    public void TryResolve_KnownId_ReturnsTrueAndTheCategory()
    {
        var target = Categories[0];

        var result = CategoryNameResolver.TryResolve(target.Id, Categories, out var category);

        result.Should().BeTrue();
        category.Should().NotBeNull();
        category!.Name.Should().Be("Mercado");
        category.Active.Should().BeTrue();
    }

    [Fact]
    public void TryResolve_KnownId_ResolvesRegardlessOfActiveFlag()
    {
        var target = Categories[2];

        var result = CategoryNameResolver.TryResolve(target.Id, Categories, out var category);

        result.Should().BeTrue();
        category!.Active.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var result = CategoryNameResolver.TryResolve(Guid.NewGuid(), Categories, out var category);

        result.Should().BeFalse();
        category.Should().BeNull();
    }

    [Fact]
    public void TryResolve_NullValue_ReturnsFalse()
    {
        var result = CategoryNameResolver.TryResolve(null, Categories, out var category);

        result.Should().BeFalse();
        category.Should().BeNull();
    }
}
