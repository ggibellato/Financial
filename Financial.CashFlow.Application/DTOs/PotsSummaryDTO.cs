namespace Financial.CashFlow.Application.DTOs;

public sealed class PotsSummaryDTO
{
    public required Guid PotsAccountId { get; init; }
    public required string Currency { get; init; }
    public required decimal Total { get; init; }
    public required IReadOnlyList<PotBalanceDTO> Pots { get; init; }
}
