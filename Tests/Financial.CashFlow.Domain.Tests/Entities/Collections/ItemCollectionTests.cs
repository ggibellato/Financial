using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    public class ItemCollectionTests
    {
        private readonly ItemCollection<BalanceAdjustment> _collection;

        private readonly BalanceAdjustment _balanceAdjustment;

        public ItemCollectionTests()
        {
             _balanceAdjustment = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), 
                 Bank.Create("Barclays", false), 2340.17m, -4.20m, "Matched against July statement");
            _collection = [];
        }

        [Fact]
        public void ItemCollection_Just_Create_Should_have_count_of_zero()
        {
            _collection.Count.Should().Be(0);
        }

        [Fact]
        public void ItemCollection_Should_be_possible_add_Item()
        {
            _collection.Add(_balanceAdjustment);

            _collection.Should().Contain(_balanceAdjustment);
        }
    }
}
