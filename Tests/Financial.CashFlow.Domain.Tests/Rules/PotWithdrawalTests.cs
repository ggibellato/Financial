using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class PotWithdrawalTests
{
    private static readonly DateOnly Date = new(2026, 10, 12);
    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly Guid OperationId = Guid.NewGuid();

    private readonly Category _reservaCategory = Category.Create("Reserva");
    private readonly Category _health = Category.Create("Saude");
    private readonly Account _bank = TestAccounts.Bank("Chase");
    private readonly Account _roundUpBank = TestAccounts.Bank("Monzo", roundUpEnabled: true);
    private readonly Account _reserva;
    private readonly Pot _pot = Pot.Create("Investimento", 100m);
    private readonly Account _card = TestAccounts.CreditCard("Barclaycard");

    public PotWithdrawalTests()
    {
        _reserva = TestAccounts.Pots("Reserva", category: _reservaCategory);
        _reserva.AddPot(_pot);
    }

    [Fact]
    public void Build_ThroughABank_WritesTheMovementAndTwoBankExpensesSharingTheOperationId()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId, Today);

        var transaction = records.Transactions.Single();
        using (new AssertionScope())
        {
            transaction.Pot.Should().BeSameAs(_pot);
            transaction.Amount.Should().Be(-38.25m);
            transaction.Date.Should().Be(Date);
            transaction.Description.Should().Be("Boots");
            transaction.OperationId.Should().Be(OperationId);
            records.Expenses.Select(expense => (expense.Category, expense.Value)).Should().Equal((_reservaCategory, -38.25m), (_health, 38.25m));
            records.Expenses.Should().OnlyContain(expense =>
                expense.PaymentSourceAccount == _bank
                && expense.CreditCard == null
                && expense.Date == Date
                && expense.Description == "Boots"
                && expense.CountsAsTithe
                && expense.RoundUpAmount == null
                && expense.OperationId == OperationId);
        }
    }

    [Fact]
    public void Build_WithoutABank_WritesOnlyTheMovement()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId, Today);

        var transaction = records.Transactions.Single();
        using (new AssertionScope())
        {
            transaction.Amount.Should().Be(-20m);
            transaction.OperationId.Should().Be(OperationId);
            records.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public void Build_DateAfterToday_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 20m, new DateOnly(2026, 11, 12), "Cash", bank: null, category: null, OperationId, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Date must not be in the future.");
    }

    [Theory]
    [InlineData(2026, 11, 11)]
    [InlineData(2026, 11, 10)]
    public void Build_DateTodayOrEarlier_IsAccepted(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);

        var records = PotWithdrawal.Build(_reserva, _pot, 20m, date, "Cash", bank: null, category: null, OperationId, Today);

        records.Date.Should().Be(date);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Build_AmountNotAboveZero_Throws(decimal amount)
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, amount, Date, "Cash", bank: null, category: null, OperationId, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Build_ThroughABankWithoutACategory_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Boots", _bank, category: null, OperationId, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Category is required when a bank is selected.");
    }

    [Fact]
    public void Build_PotOfAnotherAccount_Throws()
    {
        var holiday = TestAccounts.Pots("Holiday");

        var act = () => PotWithdrawal.Build(holiday, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId, Today);

        act.Should().Throw<ArgumentException>().WithMessage("That pot isn't in Holiday.");
    }

    [Fact]
    public void Build_ThroughABankWhenThePotsAccountHasNoCategory_Throws()
    {
        var reserva = TestAccounts.Pots("Reserva");
        reserva.AddPot(_pot);

        var act = () => PotWithdrawal.Build(reserva, _pot, 20m, Date, "Boots", _bank, _health, OperationId, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Set a category on Reserva first.");
    }

    [Fact]
    public void EnsureUsableCategory_InvestmentCategory_Throws()
    {
        var act = () => PotWithdrawal.EnsureUsableCategory(Category.Create("Investimentos", isInvestment: true), [_reserva]);

        act.Should().Throw<ArgumentException>().WithMessage("Category 'Investimentos' cannot be used for a withdrawal expense.");
    }

    [Fact]
    public void EnsureUsableCategory_AnAccountsCategory_Throws()
    {
        var act = () => PotWithdrawal.EnsureUsableCategory(_reservaCategory, [_bank, _reserva]);

        act.Should().Throw<ArgumentException>().WithMessage("Category 'Reserva' cannot be used for a withdrawal expense.");
    }

    [Fact]
    public void EnsureUsableCategory_AnOrdinaryCategory_IsAllowed()
    {
        var act = () => PotWithdrawal.EnsureUsableCategory(_health, [_bank, _reserva]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Build_WithACard_WritesTheMovementAndOneUnpaidCardChargeOnTheStatement()
    {
        var statement = new DateOnly(2026, 11, 1);

        var records = PotWithdrawal.Build(
            _reserva, _pot, 120m, Date, "Flights", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, statement));

        var charge = records.Expenses.Single();
        using (new AssertionScope())
        {
            records.Transactions.Single().Amount.Should().Be(-120m);
            records.Transactions.Single().Date.Should().Be(Date);
            charge.Value.Should().Be(120m);
            charge.Category.Should().BeSameAs(_health);
            charge.CreditCard.Should().BeSameAs(_card);
            charge.PaymentSourceAccount.Should().BeNull();
            charge.ChargeDate.Should().Be(Date);
            charge.InvoiceDate.Should().Be(statement);
            charge.CountsAsTithe.Should().BeTrue();
            charge.OperationId.Should().Be(OperationId);
        }
    }

    [Fact]
    public void Build_WithACardOnTheExpenseMonthsStatement_IsAccepted()
    {
        var records = PotWithdrawal.Build(
            _reserva, _pot, 10m, Date, "Flights", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 10, 1)));

        records.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Build_WithACardOnAStatementBeforeTheExpenseMonth_Throws()
    {
        var act = () => PotWithdrawal.Build(
            _reserva, _pot, 10m, Date, "Flights", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 9, 1)));

        act.Should().Throw<ArgumentException>().WithMessage("The statement can't be before the expense date.");
    }

    [Fact]
    public void Build_WithACardAndNoCategory_Throws()
    {
        var act = () => PotWithdrawal.Build(
            _reserva, _pot, 10m, Date, "Flights", bank: null, category: null, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 10, 1)));

        act.Should().Throw<ArgumentException>().WithMessage("Category is required when a credit card is selected.");
    }

    [Fact]
    public void Build_WithABankAndACard_Throws()
    {
        var act = () => PotWithdrawal.Build(
            _reserva, _pot, 10m, Date, "Flights", _bank, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 10, 1)));

        act.Should().Throw<ArgumentException>().WithMessage("Choose a bank or a credit card, not both.");
    }

    [Fact]
    public void Build_ThroughARoundUpBankWithARoundUp_RecordsItOnTheSpendOnly()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _roundUpBank, _health, OperationId, Today, roundUpAmount: 0.75m);

        using (new AssertionScope())
        {
            records.Transactions.Single().Amount.Should().Be(-38.25m);
            records.Expenses.Select(expense => (expense.Value, expense.RoundUpAmount)).Should().Equal((-38.25m, (decimal?)null), (38.25m, 0.75m));
            records.RoundUpAmount.Should().Be(0.75m);
            records.Expenses.Sum(expense => expense.ContributionTo(_roundUpBank)).Should().Be(-0.75m);
        }
    }

    [Fact]
    public void Build_ThroughARoundUpBankWithAZeroRoundUp_RecordsZero()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _roundUpBank, _health, OperationId, Today, roundUpAmount: 0m);

        records.RoundUpAmount.Should().Be(0m);
    }

    [Fact]
    public void Build_RoundUpOnABankWithoutRoundUp_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId, Today, roundUpAmount: 0.75m);

        act.Should().Throw<ArgumentException>().WithMessage("Bank 'Chase' does not support round-up.");
    }

    [Fact]
    public void Build_RoundUpAboveTheMaximum_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _roundUpBank, _health, OperationId, Today, roundUpAmount: 1m);

        act.Should().Throw<ArgumentException>().WithMessage("Round-up amount must be between £0.00 and £0.99.");
    }

    [Fact]
    public void Build_RoundUpWithACard_Throws()
    {
        var act = () => PotWithdrawal.Build(
            _reserva, _pot, 10m, Date, "Flights", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 10, 1)), 0.5m);

        act.Should().Throw<ArgumentException>().WithMessage("Round-up only applies to a withdrawal paid from a bank.");
    }

    [Fact]
    public void Build_RoundUpWithoutAPayment_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 10m, Date, "Cash", bank: null, category: null, OperationId, Today, roundUpAmount: 0.5m);

        act.Should().Throw<ArgumentException>().WithMessage("Round-up only applies to a withdrawal paid from a bank.");
    }
}
