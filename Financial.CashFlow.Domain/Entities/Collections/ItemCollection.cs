using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Financial.CashFlow.Domain.Entities.Collections
{
    internal class ItemCollection<T> : IReadOnlyCollection<T>
    {
        // Mutations are serialized by CashFlowJsonRepository's write gate; readers never lock because
        // each mutation publishes a new array and an enumeration holds the one it started on.
        private ImmutableArray<T> _items = ImmutableArray<T>.Empty;

        protected ImmutableArray<T> Items
        {
            get => _items;
            set => _items = value;
        }

        public int Count => _items.Length;

        internal void Add(T item)
        {
            _items = _items.Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
