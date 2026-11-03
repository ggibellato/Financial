namespace Financial.CashFlow.Application.DTOs;

public sealed class PotOperationDTO
{
    public required Guid OperationId { get; init; }
    public required string Kind { get; init; }
    public required Guid PotsAccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }
    public string? Into { get; init; }
    public Guid? PotId { get; init; }
    public IReadOnlyList<FundingLineDTO> FundingLines { get; init; } = [];
    public Guid? BankId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? IncomeId { get; init; }
}
