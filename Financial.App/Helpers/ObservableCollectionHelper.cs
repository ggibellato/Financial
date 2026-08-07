using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.Helpers;

public static class ObservableCollectionHelper
{
    public static void ReplaceAll<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
