using System;
using Financial.Domain.Entities;

namespace Financial.Application.Validation;

public static class CreditTypeParser
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        if (string.Equals(value, "Dividend", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Dividend";
            return true;
        }

        if (string.Equals(value, "Rent", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Rent";
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    public static bool TryParse(string? value, out Credit.CreditType creditType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            creditType = default;
            return false;
        }

        return Enum.TryParse(value, true, out creditType);
    }
}
