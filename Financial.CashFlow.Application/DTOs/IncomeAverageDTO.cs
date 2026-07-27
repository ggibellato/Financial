using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.DTOs
{
    public class IncomeAverageDTO
    {
        public required IncomeSource IncomeSource { get; init; }
        public decimal? GrossAverageValue { get; init; }
        public required decimal NetAverageValue { get; init; }
    }
}
