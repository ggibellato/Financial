using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.DTOs
{
    public class IncomeGroupValueDTO
    {
        public required string IncomeGroup { get; init; }
        public decimal? GrossAverageValue { get; init; }
        public required decimal NetAverageValue { get; init; }
    }
}
