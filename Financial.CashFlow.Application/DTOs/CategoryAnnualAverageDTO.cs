namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryAnnualAverageDTO
{
    public required int Year { get; init; }
    public required List<CategoryAverageDTO> AnnualAverages  { get; init; }
}
