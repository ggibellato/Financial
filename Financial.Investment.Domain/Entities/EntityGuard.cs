using System;
using System.Collections.Generic;

namespace Financial.Investment.Domain.Entities;

internal static class EntityGuard
{
    public static void EnsureNotEmptyId(Guid id, string message, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void ReplaceAll<T>(List<T> target, IReadOnlyCollection<T> data)
    {
        target.Clear();
        target.AddRange(data);
    }
}
