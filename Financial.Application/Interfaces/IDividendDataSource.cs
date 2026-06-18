using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IDividendDataSource
{
    IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker);
}
