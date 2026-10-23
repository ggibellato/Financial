namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TemplateName { get; init; }
    public required string Currency { get; init; }
    public required bool IsActive { get; init; }
}
