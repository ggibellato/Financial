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
    private static readonly Guid OperationId = Guid.NewGuid();

    private readonly Category _reservaCategory = Category.Create("Reserva");
    private readonly Category _health = Category.Create("Saude");
    private readonly Account _bank = TestAccounts.Bank("Chase");
    private readonly Account _reserva;
    private readonly ReserveBucket _pot = ReserveBucket.Create("Investimento", 100m);

    public PotWithdrawalTests()
    {
        _reserva = TestAccounts.Pots("Reserva", category: _reservaCategory);
        _reserva.AddPot(_pot);
    }

    [Fact]
    public void Build_ThroughABank_WritesTheMovementAndTwoBankExpensesSharingTheOperationId()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId);

        using (new AssertionScope())
        {
            records.Movements.Single().Bucket.Should().BeSameAs(_pot);
            records.Movements.Single().Amount.Should().Be(-38.25m);
            records.Movements.Single().Date.Should().Be(Date);
            records.Movements.Single().Description.Should().Be("Boots");
            records.Movements.Single().OperationId.Should().Be(OperationId);
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
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId);

        using (new AssertionScope())
        {
            records.Movements.Single().Amount.Should().Be(-20m);
            records.Movements.Single().OperationId.Should().Be(OperationId);
            records.Expenses.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Build_AmountNotAboveZero_Throws(decimal amount)
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, amount, Date, "Cash", bank: null, category: null, OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Build_ThroughABankWithoutACategory_Throws()
    {
        var act = () => PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Boots", _bank, category: null, OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Category is required when a bank is selected.");
    }

    [Fact]
    public void Build_PotOfAnotherAccount_Throws()
    {
        var holiday = TestAccounts.Pots("Holiday");

        var act = () => PotWithdrawal.Build(holiday, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("That pot isn't in Holiday.");
    }

    [Fact]
    public void Build_ThroughABankWhenThePotsAccountHasNoCategory_Throws()
    {
        var reserva = TestAccounts.Pots("Reserva");
        reserva.AddPot(_pot);

        var act = () => PotWithdrawal.Build(reserva, _pot, 20m, Date, "Boots", _bank, _health, OperationId);

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
}
