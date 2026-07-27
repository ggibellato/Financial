namespace Financial.CashFlow.Application.DTOs
{
    public class IncomeAnnualAverageDTO
    {
        public required int Year { get; init; }
        public required decimal SalaryAverage { get; init; }
        public required decimal SalaryAfterTaxesAverage { get; init; }
        public required decimal TaxDifferenceAverage { get; init; }
        public required decimal DividendoJurosAverage { get; init; }
    }
}
