using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Rules;

namespace Financial.Presentation.App.ViewModels.Investment;

public static class TransactionDialogValidation
{
    public static string BuildValidationMessage(bool isDeleteMode, DateTime date, string? type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld)
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

        var hasValidType = TransactionTypeParser.TryParse(type, out var parsedType);
        if (!hasValidType)
        {
            errors.Add("Type must be Buy, Sell, Fee, Redemption, Transfer In, Transfer Out, Capital Call, or Return of Capital.");
        }

        var hasQuantityEffect = !hasValidType || TransactionTypeEffects.For(parsedType).Quantity != QuantityEffect.None;

        if (hasQuantityEffect)
        {
            if (quantity <= 0)
            {
                errors.Add("Quantity must be greater than zero.");
            }

            if (unitPrice < 0)
            {
                errors.Add("Unit price cannot be negative.");
            }
        }
        else
        {
            if (quantity != 0)
            {
                errors.Add("Quantity must be zero for a type with no quantity effect.");
            }

            if (unitPrice != 0)
            {
                errors.Add("Unit price must be zero for a type with no quantity effect.");
            }
        }

        if (fees < 0)
        {
            errors.Add("Fees cannot be negative.");
        }

        if (withheld < 0)
        {
            errors.Add("Withheld cannot be negative.");
        }

        return string.Join(Environment.NewLine, errors);
    }

    public static bool IsValidTransactionType(string? value) =>
        TransactionTypeParser.TryNormalize(value, out _);
}
