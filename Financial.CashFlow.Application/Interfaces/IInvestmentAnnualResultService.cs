using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IInvestmentAnnualResultService
{
    InvestmentAnnualResultDTO GetInvestmentAnnualResultForYear(int year);
}
