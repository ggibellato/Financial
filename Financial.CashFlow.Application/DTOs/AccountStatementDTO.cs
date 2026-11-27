namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountStatementDTO
{
    public required Guid? Id { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Total { get; init; }
    public required decimal CreditIn { get; init; }
    public required decimal OutstandingTotal { get; init; }
    public required decimal CreditCarried { get; init; }
    public required string Status { get; init; }
    public required bool IsPaid { get; init; }
    public DateOnly? SettledOn { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateOnly? PaidOn { get; init; }
    public decimal? PaidAmount { get; init; }
    public string? PaidFromAccountName { get; init; }
    public decimal? PotLinkedTotal { get; init; }
    public required IReadOnlyList<StatementPotWithdrawalDTO> PotWithdrawals { get; init; }
}
