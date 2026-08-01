namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class CreateEntryFormValidation
{
    public static string BuildValidationMessage(DateTime? date, string description, string value)
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

        if (!decimal.TryParse(value, out var parsedValue) || parsedValue == 0)
        {
            errors.Add("Value must be a non-zero number.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
