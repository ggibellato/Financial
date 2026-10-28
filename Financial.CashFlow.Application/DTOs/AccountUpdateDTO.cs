namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountUpdateDTO
{
    public required string TemplateName { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }
    public decimal? OpeningBalance { get; init; }
    public DateOnly? BalanceStartDate { get; init; }
    public bool? RoundUpEnabled { get; init; }
    public Guid? CategoryId { get; init; }
    public required bool IsActive { get; init; }
}
