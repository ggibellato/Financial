using System.Collections.Generic;

namespace Financial.Infrastructure.Integrations.ImportGoogleSpreadSheets;

internal static class GoogleGeneratorConfiguration
{
    internal static readonly IReadOnlyList<string> IgnoreSheetNames = new[]
    {
        "Totais",
        "Totais com cotacao",
        "Recomendacoes",
        "Fundos de Investimento",
        "Opcoes"
    };

    internal static readonly IReadOnlyDictionary<string, string> PortfolioNameMap =
        new Dictionary<string, string>
        {
            { "Trading 212_76a5af", "Fantastic Five Divid" },
            { "Trading 212_ffd966", "Almost Daily Dividen" },
            { "XPI_f4cccc",         "Gold" },
            { "XPI_ffff",           "Acoes" },
            { "XPI_cc0000",         "Fundos Investimento" },
            { "XPI_222222",         "Encerradas" },
            { "FreeTrade_222222",   "Encerradas" },
            { "Trading 212_222222", "Encerradas" },
        };

    internal static readonly IReadOnlyDictionary<string, string> BrokerCurrencyMap =
        new Dictionary<string, string>
        {
            { "Trading 212", "GBP" },
            { "XPI",         "BRL" },
            { "FreeTrade",   "GBP" },
            { "Coinbase",    "GBP" },
        };
}
