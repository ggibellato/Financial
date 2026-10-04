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
            Items = Items.RemoveAll(i => _idSelector(i) == id);
        }

        internal void Update(T item)
        {
            var targetId = _idSelector(item);
            var items = Items;
            for (var i = 0; i < items.Length; i++)
            {
                if (_idSelector(items[i]) == targetId)
                {
                    Items = items.SetItem(i, item);
                    return;
                }
            }
        }
    }
}
