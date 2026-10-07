namespace Financial.CashFlow.Application.DTOs;

public sealed class ReserveSplitStatusDTO
{
    public required decimal ActiveTotal { get; init; }
    public string? Warning { get; init; }
}
