using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using System.Collections.Generic;

namespace Financial.Infrastructure.Integrations.ImportGoogleSpreadSheets;

internal static class GoogleGeneratorConfiguration
{
    private static readonly IReadOnlyList<string> IgnoreSheetNames = new[]
    {
        "Totais",
        "Totais com cotacao",
        "Recomendacoes",
        "Fundos de Investimento",
        "Opcoes"
    };

    private static readonly IReadOnlyDictionary<string, string> PortfolioNameMap =
        new Dictionary<string, string>
        {
            { "Trading 212_76a5af", "Fantastic Five Divid" },
            { "Trading 212_ffd966", "Almost Daily Dividen" },
            { "XPI_ffff",           "Acoes" },
            { "XPI_cc0000",         "Previdencia" },
            { "XPI_222222",         "Encerradas" },
            { "XPI_6aa84f",         "Renda Fixa" },
            { "XPI_38761d",         "Reserva" },
            { "FreeTrade_222222",   "Encerradas" },
            { "Trading 212_222222", "Encerradas" },
        };

    private static readonly IReadOnlyDictionary<string, string> BrokerCurrencyMap =
        new Dictionary<string, string>
        {
            { "Trading 212", "GBP" },
            { "XPI",         "BRL" },
            { "FreeTrade",   "GBP" },
            { "Coinbase",    "GBP" },
        };

    internal static GoogleGeneratorOptions BuildOptions() =>
        new(IgnoreSheetNames, PortfolioNameMap, BrokerCurrencyMap);
}
