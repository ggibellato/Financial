using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.TestUtilities;

public sealed class StubPaymentsDueService : IPaymentsDueService
{
    public IReadOnlyList<PaymentDueDTO> PaymentsToReturn { get; set; } = [];

    public IReadOnlyList<PaymentDueDTO> GetPaymentsDue() => PaymentsToReturn;
}
