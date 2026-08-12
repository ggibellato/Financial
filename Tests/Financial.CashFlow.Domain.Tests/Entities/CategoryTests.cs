using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities
{
    public class CategoryTests
    {
        [Fact]
        public void Create_WithValidValues_SetsAllProperties()
        {
            var category = Category.Create("Mercado", isInvestment: false, isTithe: false, isActive: true);

            using (new AssertionScope())
            {
                category.Id.Should().NotBeEmpty();
                category.Name.Should().Be("Mercado");
                category.Active.Should().BeTrue();
                category.IsInvestment.Should().BeFalse();
                category.IsTithe.Should().BeFalse();
            }
        }

        [Fact]
        public void Create_TwoCategories_HaveDifferentIds()
        {
            var first = Category.Create("Mercado");
            var second = Category.Create("Casa");

            first.Id.Should().NotBe(second.Id);
        }

        [Fact]
        public void Create_DefaultsActiveToTrueAndFlagsToFalse()
        {
            var category = Category.Create("Mercado");

            using (new AssertionScope())
            {
                category.Active.Should().BeTrue();
                category.IsInvestment.Should().BeFalse();
                category.IsTithe.Should().BeFalse();
            }
        }

        [Fact]
        public void Create_WithIsInvestmentTrue_AssignsIsInvestment()
        {
            var category = Category.Create("Investimento", isInvestment: true);

            category.IsInvestment.Should().BeTrue();
        }

        [Fact]
        public void Create_WithIsTitheTrue_AssignsIsTithe()
        {
            var category = Category.Create("Dizimo", isTithe: true);

            category.IsTithe.Should().BeTrue();
        }

        [Fact]
        public void Create_WithIsActiveFalse_AssignsFalse()
        {
            var category = Category.Create("Mercado", isActive: false);

            category.Active.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithoutAName_Throws(string? name)
        {
            var act = () => Category.Create(name!);

            act.Should().Throw<ArgumentException>();
        }
    }
}
