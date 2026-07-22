namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown when a withdrawal would take a reserve bucket's balance negative and the caller
/// hasn't confirmed it should proceed anyway.
/// </summary>
public sealed class OverdraftConfirmationRequiredException : Exception
{
    public OverdraftConfirmationRequiredException(string message) : base(message)
    {
    }
}
