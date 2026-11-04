namespace Financial.CashFlow.Application.DTOs;

public sealed class PotsUpdateDTO
{
    public required IReadOnlyList<PotUpdateDTO> Pots { get; init; }
}
