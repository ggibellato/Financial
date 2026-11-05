using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Application.Exceptions;

public sealed class StatementPaymentConflictException : Exception
{
    public StatementPaymentConflictException(string message) : base(message)
    {
    }

    public static void ThrowIfLinkedToCard(Transfer transfer)
    {
        if (transfer.IsStatementPayment)
        {
            throw new StatementPaymentConflictException("This transfer pays a card statement; unmark the statement instead.");
        }

        if (transfer.IsCreditRefund)
        {
            throw new StatementPaymentConflictException("This transfer refunds card credit; delete it from the card or bank screen instead.");
        }
    }
}
