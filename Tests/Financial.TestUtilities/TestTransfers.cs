using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.TestUtilities;

public static class TestTransfers
{
    public static Transfer SeedStatementPayment(StubCashFlowRepository repository, Account bank, DateOnly date)
    {
        var card = TestAccounts.CreditCard("Amex");
        repository.Accounts.Add(card);
        var payment = Transfer.CreateStatementPayment(date, bank, card, 40m, TestClock.Today);
        repository.Transfers.Add(payment);
        return payment;
    }
}
