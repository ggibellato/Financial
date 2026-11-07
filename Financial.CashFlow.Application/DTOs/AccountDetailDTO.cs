namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountDetailDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TemplateName { get; init; }
    public required string Currency { get; init; }
    public required bool IsActive { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Balance { get; init; }
    public required bool BalanceIsOwed { get; init; }
    public required bool HasStatements { get; init; }
    public required decimal? TotalOwed { get; init; }
    public required AccountStatementDTO? ViewedStatement { get; init; }
    public required IReadOnlyList<string> AllowedActions { get; init; }
    public required IReadOnlyDictionary<string, string> DisabledActions { get; init; }
    public required IReadOnlyList<CurrencyBalanceDTO> CurrentBalances { get; init; }
    public string? OtherParticipant { get; init; }
}
