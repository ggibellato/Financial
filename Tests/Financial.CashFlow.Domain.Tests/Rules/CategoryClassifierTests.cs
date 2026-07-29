using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class CategoryClassifierTests
{
    [Fact]
    public void IsInvestment_Investimento_ReturnsTrue()
    {
        Category.Investimento.IsInvestment().Should().BeTrue();
    }

    [Theory]
    [InlineData(Category.Mercado)]
    [InlineData(Category.Reserva)]
    [InlineData(Category.Dizimo)]
    public void IsInvestment_OtherCategories_ReturnsFalse(Category category)
    {
        category.IsInvestment().Should().BeFalse();
    }
}
