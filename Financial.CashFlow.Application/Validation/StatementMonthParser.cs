using System.Globalization;

namespace Financial.CashFlow.Application.Validation;

internal static class StatementMonthParser
{
    private const string InvalidStatementMonthMessage = "The statement month is not a valid month.";

    internal static DateOnly Parse(int year, int month) =>
        DateOnly.TryParseExact($"{year:D4}-{month:D2}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new ArgumentException(InvalidStatementMonthMessage);

    internal static DateOnly? ParseOptional(int? year, int? month) =>
        (year, month) switch
        {
            (null, null) => null,
            ({ } y, { } m) => Parse(y, m),
            _ => throw new ArgumentException(InvalidStatementMonthMessage)
        };
}
