namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class TransferFormValidation
{
    public static string BuildValidationMessage(DateTime? date, string sourceBank, string destinationBank, string amount)
    {
        var errors = new List<string>();

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceBank))
        {
            errors.Add("Source bank is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationBank))
        {
            errors.Add("Destination bank is required.");
        }
        else if (sourceBank == destinationBank)
        {
            errors.Add("Source and destination must be different banks.");
        }

        if (!decimal.TryParse(amount, out var parsedAmount) || parsedAmount <= 0)
        {
            errors.Add("Amount must be a positive number.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
