using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class CreditCardService : ICreditCardService
{
    private readonly ICashFlowRepository _repository;

    public CreditCardService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<CreditCardDTO> GetCreditCards() =>
        _repository.GetCreditCards().Select(ToDto).ToList();

    public async Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CreditCardNameResolver.TryResolve(id, _repository.GetCreditCards(), out var creditCard))
        {
            throw new KeyNotFoundException($"Credit card '{id}' was not found.");
        }

        creditCard!.UpdateDetails(request.NextInvoiceDueDate, request.IsActive);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(creditCard);
    }

    private static CreditCardDTO ToDto(CreditCard creditCard) => new()
    {
        Id = creditCard.Id,
        Name = creditCard.Name,
        IsActive = creditCard.IsActive,
        NextInvoiceDueDate = creditCard.NextInvoiceDueDate
    };
}
