using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ITaxRuleService
{
    IReadOnlyList<TaxRuleDTO> GetTaxRules();

    Task<TaxRuleDTO> CreateTaxRuleAsync(TaxRuleCreateDTO request);

    Task<TaxRuleDTO> UpdateTaxRuleAsync(Guid id, TaxRuleUpdateDTO request);

    Task DeleteTaxRuleAsync(Guid id);
}
