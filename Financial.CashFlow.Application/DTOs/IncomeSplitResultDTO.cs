namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// The 5 amounts computed and posted by a monthly income split.
/// </summary>
public sealed class IncomeSplitResultDTO
{
    public required decimal Dizimo { get; init; }
    public required decimal Investimento { get; init; }
    public required decimal HouseTreats { get; init; }
    public required decimal Ariana { get; init; }
    public required decimal Gleison { get; init; }
}
