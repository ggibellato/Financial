using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IMensaisService
{
    Task<RecurringBillDTO> CreateBillAsync(CreateRecurringBillDTO request);
    Task DeleteBillAsync(Guid id);
    IReadOnlyList<RecurringBillDTO> GetBills();
    Task<RecurringBillDTO> UpdateBillAsync(Guid id, UpdateRecurringBillDTO request);
    Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync();
}
