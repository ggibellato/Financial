namespace Financial.CashFlow.Application.DTOs;

public sealed class ExternalPaymentUpdateDTO
{
    public required DateOnly Date { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
}
