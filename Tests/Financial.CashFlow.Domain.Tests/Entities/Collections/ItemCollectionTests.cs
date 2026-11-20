using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    [Trait("Category", "Unit")]
    public class ItemCollectionTests
    {
        private class Item
        {
            public string Name { get; set; } = string.Empty;
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
        public void GetEnumerator_WhenItemAddedMidEnumeration_CompletesWithOriginalItems()
        {
            _suv.Add(_item1);
            _suv.Add(_item2);

            using var enumerator = _suv.GetEnumerator();
            enumerator.MoveNext().Should().BeTrue();
            _suv.Add(new Item { Name = "Item 3" });

            var seen = new List<Item> { enumerator.Current };
            while (enumerator.MoveNext())
            {
                seen.Add(enumerator.Current);
            }

            seen.Should().Equal(_item1, _item2);
            _suv.Should().HaveCount(3);
        }
    }
}
