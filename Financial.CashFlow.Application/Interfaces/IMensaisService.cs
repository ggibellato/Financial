using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IMensaisService
{
    Task<RecurringBillDTO> CreateBillAsync(RecurringBillCreateDTO request);
    Task DeleteBillAsync(Guid id);
    IReadOnlyList<RecurringBillDTO> GetBills();
    Task<RecurringBillDTO> UpdateBillAsync(Guid id, RecurringBillUpdateDTO request);
    Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync();
}
