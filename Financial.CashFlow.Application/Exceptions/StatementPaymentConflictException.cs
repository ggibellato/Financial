using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Application.Exceptions;

public sealed class StatementPaymentConflictException : Exception
{
    public StatementPaymentConflictException(string message) : base(message)
    {
    }

    public static void ThrowIfStatementPayment(Transfer transfer)
    {
        if (transfer.IsStatementPayment)
        {
            throw new StatementPaymentConflictException("This transfer pays a card statement; unmark the statement instead.");
        }
    }
}
