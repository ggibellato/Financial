using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class PotTransaction : IAccountTransaction
{
    public Guid Id { get; private set; }
    public Pot Pot { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Income? Income { get; private set; }
    public Guid? OperationId { get; private set; }

    private PotTransaction() { }

    public static PotTransaction Create(Pot pot, decimal amount, DateOnly date, string description, Income? income = null, Guid? operationId = null) =>
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

    public void Update(Account potsAccount, Pot pot, decimal amount, DateOnly date, string description)
    {
        ArgumentNullException.ThrowIfNull(potsAccount);
        ArgumentNullException.ThrowIfNull(pot);

        if (!potsAccount.OwnsPot(Pot.Id))
        {
            throw new ArgumentException($"This pot transaction isn't in {potsAccount.Name}.");
        }

        if (!potsAccount.OwnsPot(pot.Id))
        {
            throw new ArgumentException($"That pot isn't in {potsAccount.Name}.");
        }

        Pot = pot;
        Amount = amount;
        Date = date;
        Description = description;
    }

    public Account? OwningAccount(IEnumerable<Account> accounts) => accounts.FirstOrDefault(account => account.OwnsPot(Pot.Id));

    public decimal ContributionTo(Account account) => account.OwnsPot(Pot.Id) ? Amount : 0m;
}
