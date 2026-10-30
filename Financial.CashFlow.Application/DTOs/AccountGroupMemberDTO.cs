namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountGroupMemberDTO
{
    public required Guid AccountId { get; init; }
    public required string Name { get; init; }
    public required string TemplateName { get; init; }
    public required string Currency { get; init; }
    public required bool IsActive { get; init; }
    public required decimal Balance { get; init; }
    public decimal? RoundUpTotal { get; init; }
    public AccountGroupStatementDTO? Statement { get; init; }
    public required bool BalanceIsOwed { get; init; }
    public required decimal? TotalOwed { get; init; }
    public required AccountNextStatementDTO? NextStatement { get; init; }
}
