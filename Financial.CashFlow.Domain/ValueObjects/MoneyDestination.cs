using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.ValueObjects;

public sealed record MoneyDestination
{
    public static readonly MoneyDestination None = new(MoneyDestinationKind.None, accountId: null);
    public static readonly MoneyDestination InvestedAutomatically = new(MoneyDestinationKind.InvestedAutomatically, accountId: null);

    public MoneyDestinationKind Kind { get; }
    public Guid? AccountId { get; }

    private MoneyDestination(MoneyDestinationKind kind, Guid? accountId)
    {
        Kind = kind;
        AccountId = accountId;
    }

    public static MoneyDestination ToAccount(Guid accountId) => new(MoneyDestinationKind.Account, accountId);

    public bool IsAccount(Guid accountId) => AccountId == accountId;
}
