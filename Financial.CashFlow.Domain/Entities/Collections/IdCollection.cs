using System;

namespace Financial.CashFlow.Domain.Entities.Collections
{
    internal class IdCollection<T> : ItemCollection<T>
    {
        private readonly Func<T, Guid> _idSelector;

        internal IdCollection(Func<T, Guid> idSelector)
        {
            _idSelector = idSelector;
        }

        internal void RemoveById(Guid id)
        {
            _items.RemoveAll(i => _idSelector(i) == id);
        }

        internal void Update(T item)
        {
            var idx = _items.FindIndex(i => _idSelector(i) == _idSelector(item));
            if (idx >= 0)
            {
                _items[idx] = item;
            }
        }
    }
}