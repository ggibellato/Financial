using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class PotDepositTests
{
    private static readonly DateOnly Date = new(2026, 10, 31);
    private static readonly Guid OperationId = Guid.NewGuid();

    private readonly Category _reservaCategory = Category.Create("Reserva");
    private readonly Account _chase = TestAccounts.Bank("Chase");
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Account _reserva;
    private readonly ReserveBucket _investimento = ReserveBucket.Create("Investimento", 50m);
    private readonly ReserveBucket _houseTreats = ReserveBucket.Create("HouseTreats", 30m);
    private readonly ReserveBucket _ariana = ReserveBucket.Create("Ariana", 20m);
    private readonly ReserveBucket _holidays = ReserveBucket.Create("Holidays", 0m, isActive: false);

    public PotDepositTests()
    {
        _reserva = TestAccounts.Pots("Reserva", category: _reservaCategory);
        foreach (var pot in new[] { _investimento, _houseTreats, _ariana, _holidays })
        {
            _reserva.AddPot(pot);
        }
    }

    [Fact]
    public void Build_IntoActivePots_SplitsOnceByPercentageWithTheResidualToTheLargest()
    {
        var records = PotDeposit.Build(_reserva, 33.33m, Date, "Ramsay bonus", pot: null, [], OperationId);

        using (new AssertionScope())
        {
            records.Movements.Select(movement => (movement.Bucket, movement.Amount))
                .Should().Equal((_investimento, 16.66m), (_houseTreats, 10.00m), (_ariana, 6.67m));
            records.Movements.Should().OnlyContain(movement =>
                movement.Date == Date && movement.Description == "Ramsay bonus" && movement.OperationId == OperationId && movement.Income == null);
            records.BankExpenses.Should().BeEmpty();
        }
    }

    [Fact]
    public void Build_IntoOneInactivePot_CreditsOnlyThatPot()
    {
        var records = PotDeposit.Build(_reserva, 40m, Date, "Trip fund", _holidays, [], OperationId);

        records.Movements.Select(movement => (movement.Bucket, movement.Amount)).Should().Equal((_holidays, 40m));
    }

    [Fact]
    public void Build_WithFundingLines_WritesOneExpensePerLineInThePotsAccountsCategory()
    {
        var records = PotDeposit.Build(
            _reserva, 90m, Date, "Ramsay bonus", pot: null, [new(_chase, 85m), new(_barclays, 5m)], OperationId);

        using (new AssertionScope())
        {
            records.BankExpenses.Select(expense => (expense.PaymentSourceAccount, expense.Value))
                .Should().Equal((_chase, 85m), (_barclays, 5m));
            records.BankExpenses.Should().OnlyContain(expense =>
                expense.Category == _reservaCategory
                && expense.CreditCard == null
                && expense.Date == Date
                && expense.Description == "Deposit to Reserva"
                && !expense.CountsAsTithe
                && expense.RoundUpAmount == null
                && expense.OperationId == OperationId);
        }
    }

    [Fact]
    public void Build_FundingLinesNotAddingUp_Throws()
    {
        var act = () => PotDeposit.Build(_reserva, 90m, Date, "Bonus", pot: null, [new(_chase, 80m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Funding lines must add up to £90.00.");
    }

    [Fact]
    public void Build_MoreThanFiveFundingLines_Throws()
    {
        var lines = Enumerable.Range(0, 6).Select(_ => new PotFundingLine(_chase, 1m)).ToList();

        var act = () => PotDeposit.Build(_reserva, 6m, Date, "Bonus", pot: null, lines, OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("A deposit has at most 5 funding lines.");
    }

    [Fact]
    public void Build_FundingLineOfZero_Throws()
    {
        var act = () => PotDeposit.Build(_reserva, 10m, Date, "Bonus", pot: null, [new(_chase, 10m), new(_barclays, 0m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Funding line amounts must be greater than zero.");
    }

    [Fact]
    public void Build_FundedFromACard_Throws()
    {
        var act = () => PotDeposit.Build(_reserva, 10m, Date, "Bonus", pot: null, [new(TestAccounts.CreditCard("Amex"), 10m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Amex is a card account and can't pay.");
    }

    [Fact]
    public void Build_FundedWithoutAPotsCategory_Throws()
    {
        var reserva = TestAccounts.Pots("Savings");
        reserva.AddPot(ReserveBucket.Create("Rainy day", 100m));

        var act = () => PotDeposit.Build(reserva, 10m, Date, "Bonus", pot: null, [new(_chase, 10m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Set a category on Savings first.");
    }

    [Fact]
    public void Build_PotOfAnotherAccount_Throws()
    {
        var act = () => PotDeposit.Build(_reserva, 10m, Date, "Bonus", ReserveBucket.Create("Trip", 0m), [], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("That pot isn't in Reserva.");
    }

    [Fact]
    public void Build_NoActivePots_Throws()
    {
        var savings = TestAccounts.Pots("Savings");
        savings.AddPot(ReserveBucket.Create("Old", 0m, isActive: false));

        var act = () => PotDeposit.Build(savings, 10m, Date, "Bonus", pot: null, [], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Savings has no active pots.");
    }

    [Fact]
    public void Build_ActivePotsNotTotalling100_Throws()
    {
        _ariana.Update("Ariana", 19.99m, isActive: true);

        var act = () => PotDeposit.Build(_reserva, 90m, Date, "Bonus", pot: null, [new(_chase, 90m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Active pots must total 100% (now 99.99%).");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Build_AmountNotPositive_Throws(decimal amount)
    {
        var act = () => PotDeposit.Build(_reserva, amount, Date, "Bonus", pot: null, [], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.");
    }
}
