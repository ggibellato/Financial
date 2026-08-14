using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    public class ItemCollectionTests
    {
        private readonly ItemCollection<BalanceAdjustment> _suv;

        private readonly BalanceAdjustment _balanceAdjustment1;
        private readonly BalanceAdjustment _balanceAdjustment2;

        public ItemCollectionTests()
        {
             _balanceAdjustment1 = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), 
                 Bank.Create("Barclays", false), 2340.17m, -4.20m, "Matched against July statement");
             _balanceAdjustment2 = BalanceAdjustment.Create(new DateOnly(2026, 7, 26), 
                 Bank.Create("Chase", false), 1000.00m , -10m, "Matched against August statement");
            _suv = [];
        }

        [Fact]
        public void ItemCollection_Just_Create_Should_have_count_of_zero()
        {
            _suv.Count.Should().Be(0);
        }

        [Fact]
        public void ItemCollection_Should_be_possible_add_Item()
        {
            _suv.Add(_balanceAdjustment1);

            _suv.Should().HaveCount(1);
            _suv.Should().Contain(_balanceAdjustment1);
        }

        [Fact]
        public void ItemCollection_Should_be_possible_more_than_one_Item()
        {
            _suv.Add(_balanceAdjustment1);
            _suv.Add(_balanceAdjustment2);

            _suv.Should().HaveCount(2);
            _suv.Should().BeEquivalentTo(new[] { _balanceAdjustment1, _balanceAdjustment2 });
        }
    }
}
