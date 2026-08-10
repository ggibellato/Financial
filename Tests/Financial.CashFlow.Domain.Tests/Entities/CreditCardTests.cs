using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities
{
    public class CreditCardTests
    {
        [Fact]
        public void Create_WithValidValues_SetsAllProperties()
        {
            var card = CreditCard.Create("Investimento", isActive: true);

            using (new AssertionScope())
            {
                card.Id.Should().NotBeEmpty();
                card.Name.Should().Be("Investimento");
                card.IsActive.Should().BeTrue();
            }
        }

        [Fact]
        public void Create_TwoCards_HaveDifferentIds()
        {
            var first = CreditCard.Create("Investimento", isActive: true);
            var second = CreditCard.Create("HouseTreats", isActive: true);

            first.Id.Should().NotBe(second.Id);
        }

        [Fact]
        public void Create_DefaultsIsActiveToTrue()
        {
            var card = CreditCard.Create("Investimento", isActive: true);

            card.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Create_WithIsActiveFalse_AssignsFalse()
        {
            var card = CreditCard.Create("Investimento", isActive: false);

            card.IsActive.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithoutAName_Throws(string? name)
        {
            var act = () => CreditCard.Create(name!, isActive: true);

            act.Should().Throw<ArgumentException>();
        }
    }
}
