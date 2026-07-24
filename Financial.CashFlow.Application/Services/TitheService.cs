using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class TitheService : ITitheService
{
    private const decimal TithePercentage = 0.10m;

    private readonly ICashFlowRepository _repository;

    public TitheService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public TitheSummaryDTO GetTitheSummary(int year, int month)
    {
        var titheBase = _repository.GetIncomes()
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .Sum(i => i.NetValue);

        var calculatedTithe = titheBase * TithePercentage;

        var dizimoTotal = _repository.GetExpenses()
            .Where(e => e.Date.Year == year && e.Date.Month == month && e.Category == Category.Dizimo)
            .Sum(e => e.Value);

        return new TitheSummaryDTO
        {
            CalculatedTithe = calculatedTithe,
            TitheBalance = calculatedTithe - dizimoTotal
        };
    }
}
