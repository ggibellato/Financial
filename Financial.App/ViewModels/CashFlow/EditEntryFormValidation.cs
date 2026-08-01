namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class EditEntryFormValidation
{
    public static string BuildValidationMessage(string brlValue, string gbpValue)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(brlValue) && !decimal.TryParse(brlValue, out _))
        {
            errors.Add("BRL value must be a number.");
        }

        if (!string.IsNullOrWhiteSpace(gbpValue) && !decimal.TryParse(gbpValue, out _))
        {
            errors.Add("GBP value must be a number.");
        }

        return string.Join(Environment.NewLine, errors);
    }
}
