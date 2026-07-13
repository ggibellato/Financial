namespace Financial.Application.DTOs;

public sealed class CalculateXirrRequestDTO
{
    public IReadOnlyList<AssetCashFlowDTO> CashFlows { get; init; } = [];
    public decimal TerminalValue { get; init; }
}
