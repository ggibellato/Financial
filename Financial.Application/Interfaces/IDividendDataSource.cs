using Financial.Domain.ValueObjects;

namespace Financial.Application.Interfaces;

public interface IDividendDataSource
{
    IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker);
}
