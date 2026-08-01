namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class IncomeSplitFormValidation
{
    public static string BuildValidationMessage(DateTime? date, string amount, string description)
    {
        var errors = new List<string>();

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (!decimal.TryParse(amount, out var parsedAmount) || parsedAmount <= 0)
        {
            errors.Add("Amount must be a positive number.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
