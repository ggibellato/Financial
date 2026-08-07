namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class IncomeFormValidation
{
    public static string BuildValidationMessage(
        DateTime? date,
        Guid? incomeSource,
        string netValue,
        Guid? bank)
    {
        var errors = new List<string>();

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (incomeSource is null)
        {
            errors.Add("Source is required.");
        }

        if (!decimal.TryParse(netValue, out _))
        {
            errors.Add("Net Value must be a number.");
        }

        if (bank is null)
        {
            errors.Add("Bank is required.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
