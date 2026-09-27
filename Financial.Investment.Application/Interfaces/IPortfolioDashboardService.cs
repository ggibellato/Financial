using Financial.Investment.Application.DTOs;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Interfaces;

public interface IPortfolioDashboardService
{
    Task<PortfolioDashboardDTO> GetDashboardAsync(Currency? displayCurrency = null, Currency? brokerCurrencyFilter = null);
}
