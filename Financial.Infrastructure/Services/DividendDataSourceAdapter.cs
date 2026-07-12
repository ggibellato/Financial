using Financial.Application.Interfaces;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Services;

public sealed class DividendDataSourceAdapter : IDividendDataSource
{
    public IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker) =>
        DadosMercadoDividend.GetDividendInfo(ticker);
}
