using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class AccountGroupSummaryServiceTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly RecordingLogger<AccountGroupSummaryService> _logger;
    private readonly AccountGroupSummaryService _sut;
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Account _chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
    private readonly Account _itau = TestAccounts.Bank("Itaú", roundUpEnabled: true, currency: Currency.BRL);
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly Category _home = Category.Create("Casa");
    private readonly Account _amex = TestAccounts.CreditCard("Amex");
    private readonly IncomeSource _salary = IncomeSource.Create("Salary", IncomeGroup.Salary);
    private readonly IncomeSource _dividends = IncomeSource.Create("Dividends", IncomeGroup.NonReportable);

    public AccountGroupSummaryServiceTests()
    {
        _barclays.SetOpeningBalance(1000m, StartDate);
        _chase.SetOpeningBalance(200m, StartDate);
        _itau.SetOpeningBalance(500m, StartDate);
        _repository = new StubCashFlowRepository();
        _repository.Accounts.AddRange([_itau, _chase, _barclays]);
        _tracer = new RecordingTelemetryTracer();
        _logger = new RecordingLogger<AccountGroupSummaryService>();
        _sut = new AccountGroupSummaryService(_repository, _tracer, _logger);
    }

    [Fact]
    public void GetDefaultGroupSummary_Members_AreListedByNameWithTheirMonthEndBalance()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 10), "Shop", 50m, _groceries, _barclays, creditCard: null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 10, 2), _salary, null, 300m, _barclays));

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Year.Should().Be(2026);
            summary.Month.Should().Be(9);
            summary.Members.Select(member => member.Name).Should().Equal("Barclays", "Chase", "Itaú");
            summary.Members[0].AccountId.Should().Be(_barclays.Id);
            summary.Members[0].TemplateName.Should().Be("Bank");
            summary.Members[0].Currency.Should().Be("GBP");
            summary.Members[0].Balance.Should().Be(950m);
            summary.Members[0].Statement.Should().BeNull();
            summary.Members[0].BalanceIsOwed.Should().BeFalse();
            summary.Members[0].IsActive.Should().BeTrue();
            summary.Members[0].TotalOwed.Should().BeNull();
            summary.Members[2].Currency.Should().Be("BRL");
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_RoundUps_SumTheMemberExpensesListedInTheMonth()
    {
        AddExpenseWithRoundUp(new DateOnly(2026, 9, 3), 2.30m, 0.70m, _chase);
        AddExpenseWithRoundUp(new DateOnly(2026, 9, 28), 4.80m, 0.20m, _chase);
        AddExpenseWithRoundUp(new DateOnly(2026, 10, 1), 1.50m, 0.50m, _chase);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 5), "No round-up", 3m, _groceries, _chase, creditCard: null));

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Members.Single(member => member.Name == "Chase").RoundUpTotal.Should().Be(0.90m);
            summary.Members.Single(member => member.Name == "Barclays").RoundUpTotal.Should().Be(0m);
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_MemberWithoutRoundUpCapability_HasNullRoundUp()
    {
        var minimal = Account.CreateWithCapabilities("Basic", Currency.GBP, new HashSet<Capability> { Capability.HasOpeningBalance });
        _repository.Accounts.Add(minimal);

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        summary.Members.Single(member => member.Name == "Basic").RoundUpTotal.Should().BeNull();
    }

    [Fact]
    public void GetDefaultGroupSummary_MixedCurrencies_TotalsPerCurrencyInFirstAppearanceOrder()
    {
        AddExpenseWithRoundUp(new DateOnly(2026, 9, 3), 2.30m, 0.70m, _chase);
        AddExpenseWithRoundUp(new DateOnly(2026, 9, 4), 9.75m, 0.25m, _itau);

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Totals.Select(total => total.Currency).Should().Equal("GBP", "BRL");
            summary.Totals[0].Balance.Should().Be(1197.00m);
            summary.Totals[0].RoundUpTotal.Should().Be(0.70m);
            summary.Totals[1].Balance.Should().Be(490.00m);
            summary.Totals[1].RoundUpTotal.Should().Be(0.25m);
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_CategoryTotalsSum_UsesEveryExpenseReportedInTheMonth()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 10), "Shop", 50m, _groceries, _barclays, creditCard: null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 11), "Rent", 700m, _home, _chase, creditCard: null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 25), "Charge", 30m, _groceries, null, _amex, invoiceDate: new DateOnly(2026, 9, 1)));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 1), "Later", 99m, _groceries, _barclays, creditCard: null));

        _sut.GetDefaultGroupSummary(2026, 9).CategoryTotalsSum.Should().Be(780m);
    }

    [Fact]
    public void GetDefaultGroupSummary_Incomes_GroupBySourceInFirstIncomeOrder()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 9, 25), _dividends, null, 12.34m, _barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 9, 1), _salary, 2500m, 2000m, _barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 9, 15), _salary, null, 100m, _chase));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 31), _salary, 900m, 800m, _chase));

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.IncomeTotals.Select(income => income.Name).Should().Equal("Dividends", "Salary");
            summary.IncomeTotals[0].IncomeSourceId.Should().Be(_dividends.Id);
            summary.IncomeTotals[0].NetValue.Should().Be(12.34m);
            summary.IncomeTotals[0].GrossValue.Should().BeNull();
            summary.IncomeTotals[1].IncomeSourceId.Should().Be(_salary.Id);
            summary.IncomeTotals[1].NetValue.Should().Be(2100m);
            summary.IncomeTotals[1].GrossValue.Should().Be(2500m);
            summary.TotalIncoming.Should().Be(2112.34m);
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_MonthWithNoRecords_ReturnsMembersAndEmptyFigures()
    {
        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Members.Should().HaveCount(3);
            summary.CategoryTotalsSum.Should().Be(0m);
            summary.IncomeTotals.Should().BeEmpty();
            summary.TotalIncoming.Should().Be(0m);
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_NoAccounts_ReturnsNoMembersAndNoTotals()
    {
        _repository.Accounts.Clear();

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Members.Should().BeEmpty();
            summary.Totals.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetDefaultGroupSummary_InvalidMonth_ThrowsArgumentOutOfRange()
    {
        var act = () => _sut.GetDefaultGroupSummary(2026, 13);

        act.Should().Throw<ArgumentOutOfRangeException>();
        _tracer.ShouldHaveFailedSpan<ArgumentOutOfRangeException>("CashFlow.AccountGroupSummaryService.GetDefaultGroupSummary");
        _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public void GetDefaultGroupSummary_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetDefaultGroupSummary(2026, 9);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountGroupSummaryService.GetDefaultGroupSummary");
        _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public void GetTemplateGroups_PotsGroup_ListsThePotsAccountsByNameWithTheirMonthEndBalanceAndTotal()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var flights = ReserveBucket.Create("Flights", 100m);
        reserva.AddPot(flights);
        var holidays = TestAccounts.Pots("holidays");
        _repository.Accounts.AddRange([reserva, holidays]);
        _repository.ReserveMovements.Add(ReserveMovement.Create(flights, 120m, new DateOnly(2026, 9, 5), "Saved"));
        _repository.ReserveMovements.Add(ReserveMovement.Create(flights, 30m, new DateOnly(2026, 10, 1), "Saved later"));

        var pots = _sut.GetTemplateGroups(2026, 9).Single(group => group.TemplateName == "Pots");

        using (new AssertionScope())
        {
            pots.Members.Select(member => member.Name).Should().Equal("holidays", "Reserva");
            var reservaMember = pots.Members[1];
            reservaMember.AccountId.Should().Be(reserva.Id);
            reservaMember.TemplateName.Should().Be("Pots");
            reservaMember.Currency.Should().Be("GBP");
            reservaMember.IsActive.Should().BeTrue();
            reservaMember.Balance.Should().Be(120m);
            reservaMember.RoundUpTotal.Should().BeNull();
            reservaMember.Statement.Should().BeNull();
            reservaMember.BalanceIsOwed.Should().BeFalse();
            reservaMember.TotalOwed.Should().BeNull();
            reservaMember.NextStatement.Should().BeNull();
            pots.Members[0].Balance.Should().Be(0m);
            pots.Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 120m));
        }
    }


    private void AddExpenseWithRoundUp(DateOnly date, decimal value, decimal roundUp, Account account)
    {
        var expense = Expense.Create(date, "Coffee", value, _groceries, account, creditCard: null);
        expense.SetRoundUpAmount(roundUp);
        _repository.Expenses.Add(expense);
    }

    [Fact]
    public void GetDefaultGroupSummary_CardMember_CarriesItsUnpaidStatementOutstandingAndOwedBalance()
    {
        _repository.Accounts.Add(_amex);
        AddCharge(new DateOnly(2026, 8, 28), 12m, new DateOnly(2026, 9, 1));
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        AddCharge(new DateOnly(2026, 9, 20), 45m, new DateOnly(2026, 10, 1));
        AddCharge(new DateOnly(2026, 9, 5), 100m, new DateOnly(2026, 9, 1)).Settle(_barclays, new DateOnly(2026, 9, 25));
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 9));

        var amex = _sut.GetDefaultGroupSummary(2026, 9).Members.Single(member => member.Name == "Amex");

        using (new AssertionScope())
        {
            amex.TemplateName.Should().Be("CreditCard");
            amex.Balance.Should().Be(-87m);
            amex.BalanceIsOwed.Should().BeTrue();
            amex.RoundUpTotal.Should().BeNull();
            amex.Statement!.OutstandingTotal.Should().Be(42m);
            amex.Statement.IsPaid.Should().BeFalse();
            amex.TotalOwed.Should().Be(87m);
            amex.NextStatement.Should().BeEquivalentTo(new AccountNextStatementDTO { Year = 2026, Month = 9, OutstandingTotal = 42m });
        }
    }

    [Fact]
    public void GetTemplateGroups_Month_GroupsAccountsInTemplateOrderWithMembersByName()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var holidays = TestAccounts.Pots("holidays");
        _repository.Accounts.AddRange([reserva, _amex, holidays]);

        var groups = _sut.GetTemplateGroups(2026, 9);

        using (new AssertionScope())
        {
            groups.Select(group => group.TemplateName).Should().Equal("Bank", "CreditCard", "Pots");
            groups[0].Members.Select(member => member.Name).Should().Equal("Barclays", "Chase", "Itaú");
            groups[1].Members.Select(member => member.Name).Should().Equal("Amex");
            groups[2].Members.Select(member => member.Name).Should().Equal("holidays", "Reserva");
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountGroupSummaryService.GetTemplateGroups");
        }
    }

    [Fact]
    public void GetTemplateGroups_TemplateWithoutListedAccounts_HasNoGroup()
    {
        _repository.Accounts.Add(TestAccounts.CreditCard("Closed card", isActive: false));

        _sut.GetTemplateGroups(2026, 9).Select(group => group.TemplateName).Should().Equal("Bank");
    }

    [Fact]
    public void GetTemplateGroups_Members_CarryActiveFlagMonthEndBalanceAndCardTotalOwed()
    {
        var closed = TestAccounts.Bank("Closed", isActive: false);
        closed.SetOpeningBalance(25m, StartDate);
        _repository.Accounts.AddRange([closed, _amex]);
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        AddCharge(new DateOnly(2026, 11, 3), 50m, new DateOnly(2026, 11, 1));
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 9));

        var groups = _sut.GetTemplateGroups(2026, 9);

        using (new AssertionScope())
        {
            var closedMember = groups[0].Members.Single(member => member.Name == "Closed");
            closedMember.IsActive.Should().BeFalse();
            closedMember.Balance.Should().Be(25m);
            closedMember.TotalOwed.Should().BeNull();
            closedMember.NextStatement.Should().BeNull();
            var amex = groups[1].Members.Single();
            amex.IsActive.Should().BeTrue();
            amex.Balance.Should().Be(-30m);
            amex.BalanceIsOwed.Should().BeTrue();
            amex.TotalOwed.Should().Be(80m);
            amex.Statement!.OutstandingTotal.Should().Be(30m);
            amex.NextStatement.Should().BeEquivalentTo(new AccountNextStatementDTO { Year = 2026, Month = 9, OutstandingTotal = 30m });
        }
    }

    [Fact]
    public void GetTemplateGroups_CardWithEverythingPaid_HasNoNextStatementInAnyMonth()
    {
        _repository.Accounts.Add(_amex);
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        var statement = CardStatement.Create(_amex, 2026, 9);
        statement.MarkLegacyPaid();
        _repository.CardStatements.Add(statement);

        using (new AssertionScope())
        {
            _sut.GetTemplateGroups(2026, 9)[1].Members.Single().NextStatement.Should().BeNull();
            _sut.GetTemplateGroups(2026, 12)[1].Members.Single().NextStatement.Should().BeNull();
        }
    }

    [Fact]
    public void GetTemplateGroups_Totals_ArePerCurrencyInFirstMemberOrderAndACardGroupTotalsWhatIsOwed()
    {
        _repository.Accounts.Add(_amex);
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        AddCharge(new DateOnly(2026, 11, 3), 50m, new DateOnly(2026, 11, 1));

        var groups = _sut.GetTemplateGroups(2026, 9);

        using (new AssertionScope())
        {
            groups[0].Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 1200m), ("BRL", 500m));
            groups[1].Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 80m));
        }
    }

    [Fact]
    public void GetTemplateGroups_NoAccounts_ReturnsNoGroups()
    {
        _repository.Accounts.Clear();

        _sut.GetTemplateGroups(2026, 9).Should().BeEmpty();
    }

    [Fact]
    public void GetTemplateGroups_InvalidMonth_ThrowsArgumentOutOfRange()
    {
        var act = () => _sut.GetTemplateGroups(2026, 13);

        act.Should().Throw<ArgumentOutOfRangeException>();
        _tracer.ShouldHaveFailedSpan<ArgumentOutOfRangeException>("CashFlow.AccountGroupSummaryService.GetTemplateGroups");
    }

    [Fact]
    public void GetTemplateGroups_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetTemplateGroups(2026, 9);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountGroupSummaryService.GetTemplateGroups");
    }

    [Fact]
    public void GetDefaultGroupSummary_CardMemberWithPaidStatement_HasNothingOutstandingEvenWithUnsettledCharges()
    {
        _repository.Accounts.Add(_amex);
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        var statement = CardStatement.Create(_amex, 2026, 9);
        statement.MarkLegacyPaid();
        _repository.CardStatements.Add(statement);

        var amex = _sut.GetDefaultGroupSummary(2026, 9).Members.Single(member => member.Name == "Amex");

        amex.Statement.Should().BeEquivalentTo(new { OutstandingTotal = 0m, IsPaid = true });
    }

    [Fact]
    public void GetDefaultGroupSummary_CardMemberWithPaidStatement_IsPaid()
    {
        _repository.Accounts.Add(_amex);
        var statement = CardStatement.Create(_amex, 2026, 9);
        statement.MarkLegacyPaid();
        _repository.CardStatements.Add(statement);

        var amex = _sut.GetDefaultGroupSummary(2026, 9).Members.Single(member => member.Name == "Amex");

        amex.Statement.Should().BeEquivalentTo(new { OutstandingTotal = 0m, IsPaid = true });
    }

    [Fact]
    public void GetDefaultGroupSummary_CardMemberWithoutStatementThisMonth_IsUnpaidWithNothingOutstanding()
    {
        _repository.Accounts.Add(_amex);
        var augustStatement = CardStatement.Create(_amex, 2026, 8);
        augustStatement.MarkLegacyPaid();
        _repository.CardStatements.Add(augustStatement);

        var amex = _sut.GetDefaultGroupSummary(2026, 9).Members.Single(member => member.Name == "Amex");

        amex.Statement.Should().BeEquivalentTo(new { OutstandingTotal = 0m, IsPaid = false });
    }

    [Fact]
    public void GetDefaultGroupSummary_CardMembers_StayOutOfTheBankTotalAndSumIntoTheCardTotal()
    {
        var visa = TestAccounts.CreditCard("Visa");
        _repository.Accounts.AddRange([_amex, visa]);
        AddCharge(new DateOnly(2026, 9, 3), 30m, new DateOnly(2026, 9, 1));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 9, 4), "Shop", 15.50m, _groceries, null, visa, invoiceDate: new DateOnly(2026, 9, 1)));

        var summary = _sut.GetDefaultGroupSummary(2026, 9);

        using (new AssertionScope())
        {
            summary.Totals.Select(total => (total.Currency, total.Balance, total.CardOutstandingTotal)).Should().Equal(
                ("GBP", 1200m, 45.50m),
                ("BRL", 500m, 0m));
        }
    }

    private Expense AddCharge(DateOnly date, decimal value, DateOnly invoiceDate)
    {
        var charge = Expense.Create(date, "Charge", value, _groceries, null, _amex, invoiceDate: invoiceDate);
        _repository.Expenses.Add(charge);
        return charge;
    }
}
