using System;
using Financial.Domain.Entities;

namespace Financial.Application.Validation;

public static class OperationTypeParser
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        if (string.Equals(value, "Buy", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Buy";
            return true;
        }

        if (string.Equals(value, "Sell", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Sell";
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    public static bool TryParse(string? value, out Operation.OperationType operationType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            operationType = default;
            return false;
        }

        return Enum.TryParse(value, true, out operationType);
    }
}
