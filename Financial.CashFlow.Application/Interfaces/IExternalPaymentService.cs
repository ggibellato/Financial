using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IExternalPaymentService
{
    Task<ExternalPaymentDTO> AddPaymentAsync(Guid accountId, ExternalPaymentCreateDTO request);
    Task<ExternalPaymentDTO> UpdatePaymentAsync(Guid accountId, Guid id, ExternalPaymentUpdateDTO request);
    Task DeletePaymentAsync(Guid accountId, Guid id);
}
