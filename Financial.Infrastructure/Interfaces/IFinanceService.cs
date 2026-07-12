using Financial.Domain.ValueObjects;

namespace Financial.Infrastructure.Interfaces;

public interface IFinanceService
{
    AssetValueSnapshot GetQuote(FinanceQuoteRequest request);
}
