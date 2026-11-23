using System;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.ValueObjects;

public sealed record CashbackSetting
{
    public const decimal MaxRatePercent = 100m;
    public const string MissingRateMessage = "Enter the cashback rate.";
    public const string RateWithoutCashbackMessage = "A cashback rate can only be set while cashback is on.";
    private const decimal PercentBase = 100m;
    private const decimal PenniesInAPound = 100m;

    public static readonly CashbackSetting Off = new(ratePercent: null);

    public decimal? RatePercent { get; }
    public bool Enabled => RatePercent is not null;

    private CashbackSetting(decimal? ratePercent)
    {
        RatePercent = ratePercent;
    }

    public static CashbackSetting On(decimal ratePercent)
    {
        if (ratePercent <= 0 || ratePercent > MaxRatePercent)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Cashback rate must be more than 0% and at most {MaxRatePercent:0}%."));
        }

        if (MoneyRounding.ToCents(ratePercent) != ratePercent)
        {
            throw new ArgumentException("Cashback rate can have at most 2 decimal places.");
        }

        return new(ratePercent);
    }

    public static CashbackSetting From(bool enabled, decimal? ratePercent) =>
        (enabled, ratePercent) switch
        {
            (true, { } rate) => On(rate),
            (true, null) => throw new ArgumentException(MissingRateMessage),
            (false, null) => Off,
            (false, not null) => throw new ArgumentException(RateWithoutCashbackMessage),
        };

    public decimal SuggestionFor(decimal value) =>
        RatePercent is { } rate && value > 0
            ? Math.Floor(value * rate / PercentBase * PenniesInAPound) / PenniesInAPound
            : 0m;
}
