namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown when a caller tries to directly update or delete a reserve movement that was created by
/// an income split - it can only be changed by editing its parent income.
/// </summary>
public sealed class ReserveMovementLinkedToIncomeException : Exception
{
    public ReserveMovementLinkedToIncomeException(string message) : base(message)
    {
    }
}
