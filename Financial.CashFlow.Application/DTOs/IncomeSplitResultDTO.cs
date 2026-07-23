namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// The amounts posted to each Reserva bucket by an income split, plus their total for
/// immediate display — no need to re-sum the movement history to know how much was split.
/// </summary>
public sealed class IncomeSplitResultDTO
{
    public required decimal Investimento { get; init; }
    public required decimal HouseTreats { get; init; }
    public required decimal Ariana { get; init; }
    public required decimal Gleison { get; init; }
    public required decimal Total { get; init; }
}
