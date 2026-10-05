namespace Financial.Presentation.App.ViewModels.Investment;

public static class PriceDialogValidation
{
    public static string BuildValidationMessage(bool isDeleteMode, DateTime date, decimal price, DateTime today)
    {
        if (isDeleteMode)
        {
            return string.Empty;
        }

        var errors = new List<string>();

        if (date == DateTime.MinValue)
        {
            errors.Add("Date is required.");
        }
        else if (date.Date > today)
        {
            errors.Add("Price date cannot be in the future.");
        }

        if (price <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
