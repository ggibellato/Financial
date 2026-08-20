namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class EditReserveMovementFormValidation
{
    public static string BuildValidationMessage(Guid? bucketId, string amount, DateTime? date, string description)
    {
        var errors = new List<string>();

        if (bucketId is null)
        {
            errors.Add("Bucket is required.");
        }

        if (!decimal.TryParse(amount, out _))
        {
            errors.Add("Amount must be a number.");
        }

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
