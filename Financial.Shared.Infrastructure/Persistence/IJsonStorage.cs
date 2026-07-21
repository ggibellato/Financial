namespace Financial.Shared.Infrastructure.Persistence;

public interface IJsonStorage
{
    Task<string> ReadAsync();
    Task WriteAsync(string json);
}
