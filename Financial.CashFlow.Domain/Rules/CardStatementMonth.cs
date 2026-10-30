namespace Financial.CashFlow.Domain.Rules;

public sealed record CardStatementMonth(int Year, int Month, decimal OutstandingTotal);
