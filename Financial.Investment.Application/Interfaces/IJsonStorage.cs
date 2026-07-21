namespace Financial.Investment.Application.Interfaces;

public interface IJsonStorage
{
    Task<string> ReadAsync();
    Task WriteAsync(string json);
}
