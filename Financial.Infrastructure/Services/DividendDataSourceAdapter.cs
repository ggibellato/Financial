using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Services;

public sealed class DividendDataSourceAdapter : IDividendDataSource
{
    private readonly Func<string, List<DividendValue>> _lookup;

    public DividendDataSourceAdapter() : this(DadosMercadoDividend.GetDividendInfo)
    {
    }

    internal DividendDataSourceAdapter(Func<string, List<DividendValue>> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker) =>
        _lookup(ticker);
}
