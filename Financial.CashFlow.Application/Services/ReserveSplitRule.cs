namespace Financial.CashFlow.Application.Services;

public static class ReserveSplitRule
{
    private const decimal ExpectedTotal = 100m;
    private const decimal Tolerance = 0.01m;

    public static bool IsBalanced(decimal activeTotal) => Math.Abs(activeTotal - ExpectedTotal) <= Tolerance;
}
