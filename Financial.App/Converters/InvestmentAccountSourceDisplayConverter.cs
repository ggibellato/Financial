using System.Globalization;
using System.Windows.Data;
using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Renders an InvestmentAccount's Source column: "—" (None), the linked card's name
/// (CreditCard), or "Sum of reserve buckets" (ReserveBucketsSum). Takes the row's Source and
/// CreditCardId plus the ViewModel's fetched credit card list, since the card's display name
/// isn't denormalized onto InvestmentAccountDTO.
/// </summary>
public class InvestmentAccountSourceDisplayConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [string source, ..] || values.Length < 3)
        {
            return "—";
        }

        var creditCardId = values[1] as Guid?;
        var creditCards = values[2] as IEnumerable<CreditCardDTO>;

        return source switch
        {
            "CreditCard" => creditCards?.FirstOrDefault(c => c.Id == creditCardId)?.Name ?? "—",
            "ReserveBucketsSum" => "Sum of reserve buckets",
            _ => "—"
        };
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
