using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IPaymentsDueService
{
    IReadOnlyList<PaymentDueDTO> GetPaymentsDue();
}
