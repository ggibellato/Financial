using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    [Trait("Category", "Unit")]
    public class IdCollectionTests
    {
        private class ItemWithId
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private readonly IdCollection<ItemWithId> _suv;

        public IdCollectionTests()
        {
            _suv = new IdCollection<ItemWithId>(i => i.Id);
        }

        [Fact]
        public void IdCollection_Should_be_possible_remove_Item()
        {
            var item1 = new ItemWithId { Id = Guid.NewGuid(), Name = "Item 1" };
            _suv.Add(item1);
            var item2 = new ItemWithId { Id = Guid.NewGuid(), Name = "Item 2" };
            _suv.Add(item2);

            _suv.RemoveById(item2.Id);

            _suv.Should().Contain(item1);
            _suv.Should().NotContain(item2);
        }

        [Fact]
        public void IdCollection_Should_be_possible_update_Item()
        {
            var item = new ItemWithId { Id = Guid.NewGuid(), Name = "Item 1" };
            var updatedItem = new ItemWithId { Id = item.Id, Name = "Updated Item 1" };
            _suv.Add(item);

            _suv.Update(updatedItem);

            _suv.Should().Contain(updatedItem);
        }

        [Fact]
        public void GetEnumerator_WhenItemRemovedMidEnumeration_CompletesWithOriginalItems()
        {
            var first = new ItemWithId { Id = Guid.NewGuid(), Name = "First" };
            var second = new ItemWithId { Id = Guid.NewGuid(), Name = "Second" };
            _suv.Add(first);
            _suv.Add(second);

            using var enumerator = _suv.GetEnumerator();
            enumerator.MoveNext().Should().BeTrue();
            _suv.RemoveById(second.Id);

            var seen = new List<ItemWithId> { enumerator.Current };
            while (enumerator.MoveNext())
            {
                seen.Add(enumerator.Current);
            }

            seen.Should().Equal(first, second);
            _suv.Should().Equal(first);
        }

        [Fact]
        public void GetEnumerator_WhenItemUpdatedMidEnumeration_YieldsPreUpdateInstance()
        {
            var id = Guid.NewGuid();
            var original = new ItemWithId { Id = id, Name = "Original" };
            var replacement = new ItemWithId { Id = id, Name = "Replacement" };
            _suv.Add(original);

            using var enumerator = _suv.GetEnumerator();
            _suv.Update(replacement);

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().BeSameAs(original);
            _suv.Single().Should().BeSameAs(replacement);
        }
    }
}
