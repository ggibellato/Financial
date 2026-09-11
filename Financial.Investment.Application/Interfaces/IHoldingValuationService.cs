using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Interfaces;

public interface IHoldingValuationService
{
    HoldingValuation GetValuation(Asset asset, InvestmentScope scope);
}
