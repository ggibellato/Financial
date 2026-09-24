namespace Financial.Presentation.App.ViewModels.Investment;

public static class CorporateActionFormValidation
{
    public const string SplitTypeValue = "Split";
    public const string MergerTypeValue = "Merger";
    public const string SpinOffTypeValue = "SpinOff";

    public static string BuildValidationMessage(
        bool isDeleteMode,
        string type,
        bool isAddMode,
        DateTime effectiveDate,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string targetAssetName,
        decimal exchangeRatio,
        decimal quantityReceived,
        decimal allocationPercentage)
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

        if (type == SplitTypeValue)
        {
            if (!TryComputeSplitRatioFactor(ratioNumerator, ratioDenominator, out _))
            {
                errors.Add("Enter a valid split ratio other than 1-for-1");
            }
        }
        else if (type == MergerTypeValue)
        {
            if (isAddMode && string.IsNullOrWhiteSpace(targetAssetName))
            {
                errors.Add("Target asset is required");
            }

            if (exchangeRatio <= 0)
            {
                errors.Add("Exchange ratio must be greater than zero");
            }
        }
        else if (type == SpinOffTypeValue)
        {
            if (isAddMode && string.IsNullOrWhiteSpace(targetAssetName))
            {
                errors.Add("New asset is required");
            }

            if (quantityReceived <= 0)
            {
                errors.Add("Quantity received must be greater than zero");
            }

            if (allocationPercentage < 0 || allocationPercentage > 100)
            {
                errors.Add("Allocation percentage must be between 0 and 100");
            }
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
