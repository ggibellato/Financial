namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown when a caller tries to create or rename a reference-data entity (Bank, Category, Credit
/// Card, Income Source, ...) using a name already in use by another record of the same type.
/// </summary>
public sealed class DuplicateNameException : Exception
{
    public DuplicateNameException(string message) : base(message)
    {
    }
}
