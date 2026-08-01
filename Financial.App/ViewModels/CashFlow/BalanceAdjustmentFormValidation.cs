namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class BalanceAdjustmentFormValidation
{
    public static string BuildValidationMessage(DateTime? date, string targetBalance)
    {
        var errors = new List<string>();

        if (date is null)
        {
            errors.Add("Date is required.");
        }

        if (!decimal.TryParse(targetBalance, out var parsedTargetBalance) || parsedTargetBalance < 0)
        {
            errors.Add("Target Balance must be zero or greater.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
