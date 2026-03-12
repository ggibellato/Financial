using System.Threading.Tasks;

namespace Financial.Application.Interfaces;

public interface IJsonStorage
{
    Task<string> ReadAsync();
    Task WriteAsync(string json);
}
