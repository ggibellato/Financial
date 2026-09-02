namespace Financial.CashFlow.Application.DTOs;

public sealed class PaymentDueDTO
{
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required DateOnly DueDate { get; init; }
    public required int DaysRemaining { get; init; }
}
