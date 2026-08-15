using Financial.CashFlow.Domain.Entities.Collections;
using FluentAssertions;


namespace Financial.CashFlow.Domain.Tests.Entities.Collections
{
    public class IdCollectionTests
    {
        private class ItemWithId
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
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

    }
}
