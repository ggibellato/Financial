using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    public class ItemCollectionTests
    {

        private class Item
        {
            public string Name { get; set; }
        }


        private readonly ItemCollection<Item> _suv;

        private readonly Item _item1;
        private readonly Item _item2;

        public ItemCollectionTests()
        {
             _item1 = new Item { Name = "Item 1" };
             _item2 = new Item { Name = "Item 2" };
            _suv = new ItemCollection<Item>();
        }

        [Fact]
        public void ItemCollection_Just_Create_Should_have_count_of_zero()
        {
            _suv.Count.Should().Be(0);
        }

        [Fact]
        public void ItemCollection_Should_be_possible_add_Item()
        {
            _suv.Add(_item1);

            _suv.Should().HaveCount(1);
            _suv.Should().Contain(_item1);
        }

        [Fact]
        public void ItemCollection_Should_be_possible_more_than_one_Item()
        {
            _suv.Add(_item1);
            _suv.Add(_item2);

            _suv.Should().HaveCount(2);
            _suv.Should().BeEquivalentTo(new[] { _item1, _item2 });
        }
    }
}
