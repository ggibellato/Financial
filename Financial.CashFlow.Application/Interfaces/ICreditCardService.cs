using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICreditCardService
{
    IReadOnlyList<CreditCardDTO> GetCreditCards();

    Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request);
}
