namespace Financial.CashFlow.Application.Exceptions;

public sealed class StatementPaymentConflictException : Exception
{
    public StatementPaymentConflictException(string message) : base(message)
    {
    }
}
