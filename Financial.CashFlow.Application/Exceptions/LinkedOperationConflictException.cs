using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Application.Exceptions;

public sealed class LinkedOperationConflictException : Exception
{
    public LinkedOperationConflictException(string message) : base(message)
    {
    }

    public static void ThrowIfLinked(Expense expense)
    {
        if (expense.OperationId is not null)
        {
            throw new LinkedOperationConflictException(
                "This expense belongs to a pot deposit or withdrawal; edit it from the Pots account.");
        }
    }

    public static void ThrowIfLinked(ReserveMovement movement)
    {
        if (movement.OperationId is not null)
        {
            throw new LinkedOperationConflictException(
                "This movement belongs to a pot deposit or withdrawal; edit it from the Pots account.");
        }
    }
}
