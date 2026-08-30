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

        [Fact]
        public void Update_SetsNameNextInvoiceDueDateAndIsActive()
        {
            var card = CreditCard.Create("BaAmex", isActive: true);
            var dueDate = new DateOnly(2026, 9, 5);

            card.Update("Nubank", isActive: false, dueDate);

            using (new AssertionScope())
            {
                card.Name.Should().Be("Nubank");
                card.NextInvoiceDueDate.Should().Be(dueDate);
                card.IsActive.Should().BeFalse();
            }
        }

        [Fact]
        public void Update_NullDueDate_ClearsIt()
        {
            var card = CreditCard.Create("BaAmex", isActive: true);
            card.Update("BaAmex", isActive: true, new DateOnly(2026, 9, 5));

            card.Update("BaAmex", isActive: true, null);

            card.NextInvoiceDueDate.Should().BeNull();
        }

        [Fact]
        public void Update_DoesNotChangeId()
        {
            var card = CreditCard.Create("BaAmex", isActive: true);
            var originalId = card.Id;

            card.Update("Nubank", isActive: false, new DateOnly(2026, 9, 5));

            card.Id.Should().Be(originalId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
        {
            var card = CreditCard.Create("BaAmex", isActive: true);

            var act = () => card.Update(name!, isActive: false, new DateOnly(2026, 9, 5));

            using (new AssertionScope())
            {
                act.Should().Throw<ArgumentException>();
                card.Name.Should().Be("BaAmex");
                card.IsActive.Should().BeTrue();
                card.NextInvoiceDueDate.Should().BeNull();
            }
        }
    }
}
