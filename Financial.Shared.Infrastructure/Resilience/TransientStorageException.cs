namespace Financial.Shared.Infrastructure.Resilience;

public sealed class TransientStorageException : Exception
{
    public TransientStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
