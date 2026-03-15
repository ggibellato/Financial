using System;
using System.Collections.Generic;

namespace Financial.Presentation.Shared.ViewModels;

public static class CreditDialogValidation
{
public static string BuildValidationMessage(bool isDeleteMode, DateTime date, string? type, decimal value)
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

        if (!IsValidCreditType(type))
        {
            errors.Add("Type must be Dividend or Rent.");
        }

        if (value <= 0)
        {
            errors.Add("Value must be greater than zero.");
        }

        return string.Join(Environment.NewLine, errors);
    }

    public static bool IsValidCreditType(string? value)
    {
        return string.Equals(value, "Dividend", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Rent", StringComparison.OrdinalIgnoreCase);
    }
}
