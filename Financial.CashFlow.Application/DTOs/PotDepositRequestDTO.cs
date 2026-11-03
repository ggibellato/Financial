namespace Financial.CashFlow.Application.DTOs;

public sealed class PotDepositRequestDTO
{
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public Guid? PotId { get; init; }
    public IReadOnlyList<FundingLineDTO> FundingLines { get; init; } = [];
}
