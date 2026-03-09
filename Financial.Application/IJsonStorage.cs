using System.Threading.Tasks;

namespace FinancialModel.Application;

public interface IJsonStorage
{
    Task<string> ReadAsync();
    Task WriteAsync(string json);
}
