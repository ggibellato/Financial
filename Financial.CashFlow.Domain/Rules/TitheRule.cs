namespace Financial.CashFlow.Domain.Rules;

/// <summary>
/// The single source of truth for the 10% tithe rate, shared by every calculation that depends
/// on it (monthly tithe calculation, income reserve split base) so the rate is never duplicated.
/// </summary>
public static class TitheRule
{
    public const decimal Percentage = 0.10m;

    public static decimal CalculateTithe(decimal amount) => amount * Percentage;

    public static decimal NetOfTithe(decimal amount) => amount - CalculateTithe(amount);
}
