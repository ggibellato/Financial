using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICreditCardService
{
    IReadOnlyList<CreditCardDTO> GetCreditCards();

    Task<CreditCardDTO> CreateCreditCardAsync(CreditCardCreateDTO request);

    Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request);

    Task DeleteCreditCardAsync(Guid id);
}
