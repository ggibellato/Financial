using System.Globalization;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
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
    private readonly Pot _investimento = Pot.Create("Investimento", 50m);
    private readonly Pot _houseTreats = Pot.Create("HouseTreats", 30m);
    private readonly Pot _ariana = Pot.Create("Ariana", 20m);
    private readonly Pot _holidays = Pot.Create("Holidays", 0m, isActive: false);

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
            records.Transactions.Select(transaction => (transaction.Pot, transaction.Amount))
                .Should().Equal((_investimento, 16.66m), (_houseTreats, 10.00m), (_ariana, 6.67m));
            records.Transactions.Should().OnlyContain(transaction =>
                transaction.Date == Date && transaction.Description == "Ramsay bonus" && transaction.OperationId == OperationId && transaction.Income == null);
            records.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public void Build_IntoOneInactivePot_CreditsOnlyThatPot()
    {
        var records = PotDeposit.Build(_reserva, 40m, Date, "Trip fund", _holidays, [], OperationId);

        records.Transactions.Select(transaction => (transaction.Pot, transaction.Amount)).Should().Equal((_holidays, 40m));
    }

    [Fact]
    public void Build_WithFundingLines_WritesOneExpensePerLineInThePotsAccountsCategory()
    {
        var records = PotDeposit.Build(
            _reserva, 90m, Date, "Ramsay bonus", pot: null, [new(_chase, 85m), new(_barclays, 5m)], OperationId);

        using (new AssertionScope())
        {
            records.Expenses.Select(expense => (expense.PaymentSourceAccount, expense.Value))
                .Should().Equal((_chase, 85m), (_barclays, 5m));
            records.Expenses.Should().OnlyContain(expense =>
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
        reserva.AddPot(Pot.Create("Rainy day", 100m));

        var act = () => PotDeposit.Build(reserva, 10m, Date, "Bonus", pot: null, [new(_chase, 10m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Set a category on Savings first.");
    }

    [Fact]
    public void Build_PotOfAnotherAccount_Throws()
    {
        var act = () => PotDeposit.Build(_reserva, 10m, Date, "Bonus", Pot.Create("Trip", 0m), [], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("That pot isn't in Reserva.");
    }

    [Fact]
    public void Build_NoActivePots_Throws()
    {
        var savings = TestAccounts.Pots("Savings");
        savings.AddPot(Pot.Create("Old", 0m, isActive: false));

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

    [Fact]
    public void BuildFromIncome_WithDefaultFunding_LinksEveryRecordToTheIncome()
    {
        var income = IncomeOf(net: 100.01m);

        var operation = PotDeposit.BuildFromIncome(income, _reserva, [], OperationId);

        using (new AssertionScope())
        {
            operation.Amount.Should().Be(90.01m);
            operation.Date.Should().Be(Date);
            operation.Description.Should().Be("Bonus");
            operation.IncomeId.Should().Be(income.Id);
            operation.Transactions.Should().OnlyContain(transaction => transaction.Income == income && transaction.OperationId == OperationId);
            var expense = operation.Expenses.Should().ContainSingle().Which;
            expense.Description.Should().Be("Deposit to Reserva \u2013 Ramsay");
            expense.Value.Should().Be(90.01m);
            expense.Date.Should().Be(Date);
            expense.Category.Should().Be(_reservaCategory);
            expense.PaymentSourceAccount.Should().Be(_chase);
            expense.CountsAsTithe.Should().BeFalse();
            expense.RoundUpAmount.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("100", "90")]
    [InlineData("123.45", "111.11")]
    [InlineData("0.05", "0.05")]
    [InlineData("1963", "1766.70")]
    [InlineData("100.005", "90.00")]
    public void BuildFromIncome_Amount_MatchesTheFiguresTheWebFormShows(string net, string deposit)
    {
        var income = IncomeOf(net: decimal.Parse(net, CultureInfo.InvariantCulture));

        var operation = PotDeposit.BuildFromIncome(income, _reserva, [], OperationId);

        operation.Amount.Should().Be(decimal.Parse(deposit, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BuildFromIncome_WithoutADescription_LeavesTheTransactionsDescriptionEmpty()
    {
        var operation = PotDeposit.BuildFromIncome(IncomeOf(net: 100m, description: null), _reserva, [], OperationId);

        operation.Transactions.Should().OnlyContain(transaction => transaction.Description == string.Empty);
    }

    [Fact]
    public void BuildFromIncome_WithHandSplitLines_WritesThemInsteadOfTheDefault()
    {
        var operation = PotDeposit.BuildFromIncome(IncomeOf(net: 100m), _reserva, [new(_barclays, 85m), new(_chase, 5m)], OperationId);

        operation.FundingLines.Select(line => (line.Bank, line.Amount)).Should().Equal((_barclays, 85m), (_chase, 5m));
    }

    [Fact]
    public void BuildFromIncome_WithLinesThatDoNotAddUp_IsRejected()
    {
        var act = () => PotDeposit.BuildFromIncome(IncomeOf(net: 100m), _reserva, [new(_chase, 80m)], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Funding lines must add up to \u00a390.00.");
    }

    [Fact]
    public void BuildFromIncome_WithoutABank_IsRejected()
    {
        var income = Income.Create(Date, IncomeSource.Create("Ramsay", IncomeGroup.Salary, potsAccount: _reserva), null, 100m, account: null);

        var act = () => PotDeposit.BuildFromIncome(income, _reserva, [], OperationId);

        act.Should().Throw<ArgumentException>().WithMessage("Choose the bank this income landed in.");
    }

    [Theory]
    [InlineData(100, 90)]
    [InlineData(2450, 2205)]
    [InlineData(100.05, 90.05)]
    [InlineData(0.05, 0.05)]
    public void BuildFromIncome_AmountIsNinetyPercentOfNetRoundedAwayFromZero(double net, double expected)
    {
        var operation = PotDeposit.BuildFromIncome(IncomeOf(net: (decimal)net), _reserva, [], OperationId);

        operation.Amount.Should().Be((decimal)expected);
    }

    [Fact]
    public void OlderIncomeTransactions_SplitsWithoutTheLinkOrTheHundredPercentRule()
    {
        _ariana.Update("Ariana", 0m, isActive: false);
        var income = IncomeOf(net: 100m, description: null, potsAccount: _reserva);

        var transactions = PotDeposit.OlderIncomeTransactions(income);

        using (new AssertionScope())
        {
            transactions.Select(transaction => (transaction.Pot, transaction.Amount)).Should().Equal((_investimento, 45m), (_houseTreats, 27m));
            transactions.Should().OnlyContain(transaction =>
                transaction.Income == income && transaction.OperationId == null && transaction.Date == Date && transaction.Description == string.Empty);
        }
    }

    [Fact]
    public void OlderIncomeTransactions_WithNoPotsAccount_IsEmpty()
    {
        PotDeposit.OlderIncomeTransactions(IncomeOf(net: 100m)).Should().BeEmpty();
    }

    private Income IncomeOf(decimal net, string? description = "Bonus", Account? potsAccount = null) =>
        Income.Create(Date, IncomeSource.Create("Ramsay", IncomeGroup.Salary, potsAccount: _reserva), null, net, _chase, description, potsAccount);
}
