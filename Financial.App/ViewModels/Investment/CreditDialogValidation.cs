using Financial.Investment.Application.Validation;

namespace Financial.Presentation.App.ViewModels.Investment;

public static class CreditDialogValidation
{
    public static string BuildValidationMessage(bool isDeleteMode, DateTime date, string? type, decimal value, decimal withheld)
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

        var withinMagnitude = value > 0 ? withheld >= 0 && withheld <= value : withheld <= 0 && withheld >= value;
        if (!withinMagnitude)
        {
            errors.Add("Withheld must share Value's sign and must not exceed it in magnitude.");
        }

        return string.Join(Environment.NewLine, errors);
    }

    public static bool IsValidCreditType(string? value) =>
        CreditTypeParser.TryNormalize(value, out _);
}
