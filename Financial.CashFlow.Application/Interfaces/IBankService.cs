using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBankService
{
    IReadOnlyList<BankDTO> GetBanks();
    Task<BankDTO> UpdateOpeningBalanceAsync(string name, BankOpeningBalanceUpdateDTO request);
    IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month);
    decimal GetBankBalanceAsOf(string bankName, DateOnly asOfDate, Guid? excludingAdjustmentId = null);
}
