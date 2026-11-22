namespace Financial.CashFlow.Application.DTOs;

public sealed class InterestRateDTO
{
    public required decimal AnnualPercent { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
}
