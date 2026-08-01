namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class AddBillFormValidation
{
    private const int MinDueDay = 1;
    private const int MaxDueDay = 31;

    public static string BuildValidationMessage(string description, string dueDay, string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        if (!int.TryParse(dueDay, out var parsedDueDay) || parsedDueDay < MinDueDay || parsedDueDay > MaxDueDay)
        {
            errors.Add($"Due Day must be a whole number between {MinDueDay} and {MaxDueDay}.");
        }

        if (!decimal.TryParse(value, out _))
        {
            errors.Add("Value must be a number.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
