namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown when a caller tries to delete a reference-data entity (Bank, Category, Credit Card, Income
/// Source, ...) that is still referenced by another record, so deleting it would orphan that record.
/// </summary>
public sealed class EntityInUseException : Exception
{
    public EntityInUseException(string message) : base(message)
    {
    }
}
