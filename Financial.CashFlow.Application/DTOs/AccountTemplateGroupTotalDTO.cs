namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTemplateGroupTotalDTO
{
    public required string Currency { get; init; }
    public required decimal Total { get; init; }
}
