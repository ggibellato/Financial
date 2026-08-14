using System;
using System.Collections;
using System.Collections.Generic;

namespace Financial.CashFlow.Domain.Entities.Collections
{
    public class ItemCollection<T> : IReadOnlyCollection<T>
    {
        private List<T> _items = new List<T>();

        public int Count => _items.Count;

        public void Add(T item)
        {
            _items.Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}