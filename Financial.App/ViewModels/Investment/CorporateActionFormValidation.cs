namespace Financial.Presentation.App.ViewModels.Investment;

public static class CorporateActionFormValidation
{
    public const string SplitTypeValue = "Split";

    public static string BuildValidationMessage(
        bool isDeleteMode,
        DateTime effectiveDate,
        decimal ratioNumerator,
        decimal ratioDenominator)
    {
        if (isDeleteMode)
        {
            return string.Empty;
        }

        var errors = new List<string>();

        if (effectiveDate == DateTime.MinValue)
        {
            errors.Add("Effective date is required");
        }

        if (!TryComputeSplitRatioFactor(ratioNumerator, ratioDenominator, out _))
        {
            errors.Add("Enter a valid split ratio other than 1-for-1");
        }

        return string.Join(Environment.NewLine, errors);
    }

    public static bool TryComputeSplitRatioFactor(decimal ratioNumerator, decimal ratioDenominator, out decimal ratioFactor)
    {
        if (ratioNumerator <= 0 || ratioDenominator <= 0)
        {
            ratioFactor = 0m;
            return false;
        }

        ratioFactor = ratioNumerator / ratioDenominator;
        return ratioFactor != 1.0m;
    }
}
