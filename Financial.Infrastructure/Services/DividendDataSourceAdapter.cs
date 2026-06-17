using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.WebPageParser;
using System.Collections.Generic;

namespace Financial.Infrastructure.Services;

public sealed class DividendDataSourceAdapter : IDividendDataSource
{
    public IReadOnlyList<DividendValue> GetDividends(string ticker) =>
        DadosMercadoDividend.GetDividendInfo(ticker);
}
