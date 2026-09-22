using Financial.Investment.Application.Validation;

namespace Financial.Presentation.App.ViewModels.Investment;

public static class CreditDialogValidation
{
    public static string BuildValidationMessage(bool isDeleteMode, DateTime date, string? type, decimal value, decimal withheld, decimal? sharesForDividend = null, decimal intermediationFee = 0m)
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

        if (!IsValidCreditType(type))
        {
            errors.Add("Type must be Dividend, Securities Lending Income, JCP, or Coupon.");
        }

        if (value == 0)
        {
            errors.Add("Value must not be zero.");
        }

        if (!IsWithinMagnitude(value, withheld))
        {
            errors.Add("Withheld must share Value's sign and must not exceed it in magnitude.");
        }

        if (!IsWithinMagnitude(value, intermediationFee))
        {
            errors.Add("Intermediation fee must share Value's sign and must not exceed it in magnitude.");
        }

        if (!IsWithinMagnitude(value, withheld + intermediationFee))
        {
            errors.Add("Withheld and intermediation fee combined must not exceed Value's magnitude.");
        }

        if (sharesForDividend is <= 0)
        {
            errors.Add("Shares for this dividend must be greater than zero when provided.");
        }

        return string.Join(Environment.NewLine, errors);
    }

    private static bool IsWithinMagnitude(decimal value, decimal deduction) =>
        value > 0 ? deduction >= 0 && deduction <= value : deduction <= 0 && deduction >= value;

    public static bool IsValidCreditType(string? value) =>
        CreditTypeParser.TryNormalize(value, out _);
}
