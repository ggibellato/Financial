using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IMensaisService
{
    Task<RecurringBillTemplateDTO> CreateTemplateAsync(CreateRecurringBillTemplateDTO request);
    IReadOnlyList<RecurringBillTemplateDTO> GetTemplates();
    Task<IReadOnlyList<RecurringBillInstanceDTO>> GetInstancesForMonthAsync(int year, int month);
    Task<RecurringBillInstanceDTO> UpdateInstanceAsync(Guid id, UpdateRecurringBillInstanceDTO request);
}
