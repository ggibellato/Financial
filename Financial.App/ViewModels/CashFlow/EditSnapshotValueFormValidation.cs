namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class EditSnapshotValueFormValidation
{
    public static string BuildValidationMessage(string value)
    {
        var errors = new List<string>();

        if (!decimal.TryParse(value, out var parsedValue) || parsedValue < 0)
        {
            errors.Add("Value must be a non-negative number.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
