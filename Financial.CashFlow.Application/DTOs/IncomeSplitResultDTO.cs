namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// The amounts computed by a monthly income split. Dizimo (tithe) is informational only — it is
/// not posted as a Reserva bucket movement. The remaining 4 amounts are posted to their buckets.
/// </summary>
public sealed class IncomeSplitResultDTO
{
    public required decimal Dizimo { get; init; }
    public required decimal Investimento { get; init; }
    public required decimal HouseTreats { get; init; }
    public required decimal Ariana { get; init; }
    public required decimal Gleison { get; init; }
}
