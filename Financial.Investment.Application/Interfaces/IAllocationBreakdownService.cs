using Financial.Investment.Application.DTOs;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Interfaces;

public interface IAllocationBreakdownService
{
    Task<AllocationBreakdownDTO> GetAllocationBreakdownAsync(Currency? displayCurrency = null, Currency? brokerCurrencyFilter = null);
}
