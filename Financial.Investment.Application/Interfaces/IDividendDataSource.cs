using Financial.Investment.Domain.ValueObjects;

namespace Financial.Investment.Application.Interfaces;

public interface IDividendDataSource
{
    IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker);
}
