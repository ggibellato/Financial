using System;
using System.Collections.Generic;

namespace Financial.Presentation.App.ViewModels;

public static class OperationDialogValidation
{
public static string BuildValidationMessage(bool isDeleteMode, DateTime date, string? type, decimal quantity, decimal unitPrice, decimal fees)
    {
        if (isDeleteMode)
        {
            return string.Empty;
        }

        var errors = new List<string>();

        if (date == DateTime.MinValue)
        {
            errors.Add("Date is required.");
        }

        if (!IsValidOperationType(type))
        {
            errors.Add("Type must be Buy or Sell.");
        }

        if (quantity <= 0)
        {
            errors.Add("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            errors.Add("Unit price cannot be negative.");
        }

        if (fees < 0)
        {
            errors.Add("Fees cannot be negative.");
        }

        return string.Join(Environment.NewLine, errors);
    }

    public static bool IsValidOperationType(string? value)
    {
        return string.Equals(value, "Buy", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Sell", StringComparison.OrdinalIgnoreCase);
    }
}

