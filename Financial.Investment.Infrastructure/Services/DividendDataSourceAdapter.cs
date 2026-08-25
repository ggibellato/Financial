using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.ValueObjects;
using Financial.Integrations.WebPageParser;

namespace Financial.Investment.Infrastructure.Services;

public sealed class DividendDataSourceAdapter : IDividendDataSource
{
    private readonly Func<string, List<DividendValue>> _lookup;

    public DividendDataSourceAdapter()
        : this(ticker => WebPageParserMappers.ToDividendValues(DadosMercadoDividend.GetDividendInfo(ticker)))
    {
    }

    internal DividendDataSourceAdapter(Func<string, List<DividendValue>> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public IReadOnlyList<DividendValue> GetDividends(string ticker) =>
        _lookup(ticker);
}
