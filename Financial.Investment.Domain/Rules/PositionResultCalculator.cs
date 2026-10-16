namespace Financial.Investment.Domain.Rules;

public sealed record PositionResult(decimal? ResultPercent, decimal? ResultWithCreditsPercent);

public static class PositionResultCalculator
{
    private static readonly PositionResult Unavailable = new(null, null);

    public static PositionResult ForOpenPosition(decimal? unrealisedGain, decimal costOfUnitsHeld, decimal totalCredits) =>
        unrealisedGain is null || costOfUnitsHeld == 0m
            ? Unavailable
            : new PositionResult(
                Percent(unrealisedGain.Value, costOfUnitsHeld),
                Percent(unrealisedGain.Value + totalCredits, costOfUnitsHeld));

    // RealizedGainLoss already includes the credits, so the credits-free figure subtracts them back out.
    public static PositionResult ForClosedPosition(decimal realizedGainLoss, decimal totalCredits, decimal totalBought) =>
        totalBought == 0m
            ? Unavailable
            : new PositionResult(
                Percent(realizedGainLoss - totalCredits, totalBought),
                Percent(realizedGainLoss, totalBought));

    private static decimal Percent(decimal gain, decimal basis) => gain / basis * 100m;
}
