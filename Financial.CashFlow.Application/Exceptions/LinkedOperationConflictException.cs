namespace Financial.CashFlow.Application.Exceptions;

public sealed class LinkedOperationConflictException : Exception
{
    public LinkedOperationConflictException(string message) : base(message)
    {
    }

    public static void ThrowIfLinked(Guid? operationId, string record)
    {
        if (operationId is not null)
        {
            throw new LinkedOperationConflictException(
                $"This {record} belongs to a pot deposit or withdrawal; edit it from the Pots account.");
        }
    }
}
