namespace Financial.Infrastructure.Interfaces;

public sealed class AssetValueNotFoundException : Exception
{
    public AssetValueNotFoundException(string message) : base(message)
    {
    }
}
