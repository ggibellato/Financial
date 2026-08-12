namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class ExpenseFormValidation
{
    public const decimal MinRoundUpAmount = 0m;
    public const decimal MaxRoundUpAmount = 0.99m;

    public static string BuildValidationMessage(
        DateTime? date,
        string description,
        string category,
        string value,
        bool isCardMode,
        Guid? paymentSource,
        Guid? creditCardId,
        bool showRoundUpField,
        string roundUpAmount)
    {
        var errors = new List<string>();

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            errors.Add("Category is required.");
        }

        if (!decimal.TryParse(value, out var parsedValue) || parsedValue == 0)
        {
            errors.Add("Value must be a non-zero number.");
        }

        if (isCardMode)
        {
            if (creditCardId is null)
            {
                errors.Add("Card is required when charging to a card.");
            }
        }
        else if (paymentSource is null)
        {
            errors.Add("Payment Source is required.");
        }

        if (showRoundUpField && !string.IsNullOrWhiteSpace(roundUpAmount))
        {
            if (!decimal.TryParse(roundUpAmount, out var parsedRoundUp)
                || parsedRoundUp < MinRoundUpAmount
                || parsedRoundUp > MaxRoundUpAmount)
            {
                errors.Add($"Round-up amount must be between £{MinRoundUpAmount:N2} and £{MaxRoundUpAmount:N2}.");
            }
        }

        return string.Join(Environment.NewLine, errors);
    }
}
