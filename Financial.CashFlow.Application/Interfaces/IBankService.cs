using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBankService
{
    IReadOnlyList<BankDTO> GetBanks();
    Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request);
    IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month);
    decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null);
}
