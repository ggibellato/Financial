namespace Financial.Investment.Application.DTOs;

public sealed class TransactionTypeEffectDTO
{
    public required string Type { get; set; }
    public required string QuantityEffect { get; set; }
    public required string CashEffect { get; set; }
}
