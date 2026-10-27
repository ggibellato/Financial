namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountStatementDTO
{
    public required Guid Id { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Total { get; init; }
    public required decimal OutstandingTotal { get; init; }
    public required bool IsPaid { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateOnly? PaidOn { get; init; }
    public string? PaidFromAccountName { get; init; }
}
