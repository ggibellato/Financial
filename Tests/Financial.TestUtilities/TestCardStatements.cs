using Financial.CashFlow.Domain.Entities;

namespace Financial.TestUtilities;

public static class TestCardStatements
{
    public static CardStatement MarkLegacyPaid(this CardStatement statement)
    {
        typeof(CardStatement).GetProperty(nameof(CardStatement.IsPaid))!.SetValue(statement, true);
        return statement;
    }
}
