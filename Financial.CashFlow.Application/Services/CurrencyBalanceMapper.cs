using Financial.CashFlow.Application.DTOs;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Application.Services;

internal static class CurrencyBalanceMapper
{
    internal static List<CurrencyBalanceDTO> ToDtos(IEnumerable<(Currency Currency, decimal Balance)> balances) =>
        balances.Select(balance => new CurrencyBalanceDTO { Currency = balance.Currency.ToString(), Balance = balance.Balance }).ToList();
}
