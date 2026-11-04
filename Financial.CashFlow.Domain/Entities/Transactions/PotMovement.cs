using System;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class PotMovement : IAccountTransaction
{
    public Guid Id { get; private set; }
    public Pot Pot { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Income? Income { get; private set; }
    public Guid? OperationId { get; private set; }

    private PotMovement() { }

    public static PotMovement Create(Pot pot, decimal amount, DateOnly date, string description, Income? income = null, Guid? operationId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Pot = pot,
            Amount = amount,
            Date = date,
            Description = description,
            Income = income,
            OperationId = operationId
        };

    public void Update(Pot pot, decimal amount, DateOnly date, string description)
    {
        Pot = pot;
        Amount = amount;
        Date = date;
        Description = description;
    }

    public decimal ContributionTo(Account account) => account.OwnsPot(Pot.Id) ? Amount : 0m;
}
