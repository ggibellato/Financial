namespace Financial.Investment.Domain.Entities;

public enum ValuationMethod
{
    Unspecified = 0,
    MarketPrice,
    NAV,
    ProviderValue,
    Manual,
    BondQuote
}
