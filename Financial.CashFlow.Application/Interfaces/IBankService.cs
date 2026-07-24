using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBankService
{
    IReadOnlyList<BankDTO> GetBanks();
    Task<BankDTO> UpdateOpeningBalanceAsync(string name, BankOpeningBalanceUpdateDTO request);
}
