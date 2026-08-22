namespace Financial.Shared.Abstractions.Resilience;

public sealed class TransientStorageException : Exception
{
    public TransientStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
