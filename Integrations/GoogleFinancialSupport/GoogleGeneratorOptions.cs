using System.Collections.Generic;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed record GoogleGeneratorOptions(
    IReadOnlyList<string> IgnoreSheetNames,
    IReadOnlyDictionary<string, string> PortfolioNameMap,
    IReadOnlyDictionary<string, string> BrokerCurrencyMap);
