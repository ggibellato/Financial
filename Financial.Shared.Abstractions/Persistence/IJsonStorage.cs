namespace Financial.Shared.Abstractions.Persistence;

public interface IJsonStorage
{
    Task<string> ReadAsync();
    Task WriteAsync(string json);
}
