namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class EditBillFormValidation
{
    public static string BuildValidationMessage(string value, string status)
    {
        var errors = new List<string>();

        if (!decimal.TryParse(value, out _))
        {
            errors.Add("Value must be a number.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            errors.Add("Status is required.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
