using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Financial.CashFlow.Domain.Entities.Collections
{
    internal class ItemCollection<T> : IReadOnlyCollection<T>
    {
        // Writers must be serialized by CashFlowJsonRepository's write gate; readers never lock.
        protected ImmutableArray<T> Items { get; set; } = ImmutableArray<T>.Empty;

        public int Count => Items.Length;

        internal void Add(T item)
        {
            Items = Items.Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
