namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryAnnualGroupValueDTO
{
    public required int Year { get; init; }
    public required List<CategoryGroupValueDTO> AnnualAverages  { get; init; }
}
