using System.Collections.Generic;

namespace Financial.Investment.SpreadsheetImport;

public sealed record GoogleGeneratorOptions(
    IReadOnlyList<string> IgnoreSheetNames,
    IReadOnlyDictionary<string, string> PortfolioNameMap,
    IReadOnlyDictionary<string, string> BrokerCurrencyMap);
