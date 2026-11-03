namespace Financial.CashFlow.Application.DTOs;

public sealed class PotWithdrawalRequestDTO
{
    public required Guid PotId { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public Guid? BankId { get; init; }
    public Guid? CategoryId { get; init; }
    public bool Confirmed { get; init; }
}
