using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class AccountQueryServiceTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateOnly Today = new(2026, 12, 31);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly RecordingLogger<AccountQueryService> _logger;
    private readonly AccountQueryService _sut;
    private readonly Account _barclays = TestAccounts.Bank("Barclays", roundUpEnabled: true);
    private readonly Account _chase = TestAccounts.Bank("chase");
    private readonly Account _closed = Account.CreateWithCapabilities(
        "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly Account _amex = TestAccounts.CreditCard("Amex");
    private readonly IncomeSource _salary = IncomeSource.Create("Salary", IncomeGroup.Salary);

    public AccountQueryServiceTests()
    {
        _barclays.SetOpeningBalance(1000m, StartDate);
        _chase.SetOpeningBalance(200m, StartDate);
        _repository = new StubCashFlowRepository();
        _repository.Accounts.AddRange([_closed, _chase, _barclays]);
        _tracer = new RecordingTelemetryTracer();
        _logger = new RecordingLogger<AccountQueryService>();
        _sut = new AccountQueryService(_repository, _tracer, _logger);
    }

    [Fact]
    public void GetAccounts_MixedCaseNames_ListsEveryAccountByNameIgnoringCase()
    {
        var accounts = _sut.GetAccounts();

        using (new AssertionScope())
        {
            accounts.Select(account => account.Name).Should().Equal("Barclays", "chase", "Closed");
            accounts[0].Id.Should().Be(_barclays.Id);
            accounts[0].TemplateName.Should().Be("Bank");
            accounts[0].Currency.Should().Be("GBP");
            accounts[0].IsActive.Should().BeTrue();
            accounts[2].IsActive.Should().BeFalse();
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetAccounts");
        }
    }

    [Fact]
    public void GetAccounts_SameNameIgnoringCase_KeepsRepositoryOrder()
    {
        var upper = TestAccounts.Bank("SAVINGS");
        var lower = TestAccounts.Bank("savings");
        _repository.Accounts.Clear();
        _repository.Accounts.AddRange([upper, lower]);

        _sut.GetAccounts().Select(account => account.Id).Should().Equal(upper.Id, lower.Id);
    }

    [Fact]
    public void GetAccount_Bank_ReturnsMonthEndBalanceAndAllActions()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 10), "Shop", 50m, _groceries, _barclays, creditCard: null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 10, 2), _salary, null, 300m, _barclays));

        var detail = _sut.GetAccount(_barclays.Id, 2026, 9);

        using (new AssertionScope())
        {
            detail.Id.Should().Be(_barclays.Id);
            detail.Name.Should().Be("Barclays");
            detail.TemplateName.Should().Be("Bank");
            detail.Currency.Should().Be("GBP");
            detail.IsActive.Should().BeTrue();
            detail.Year.Should().Be(2026);
            detail.Month.Should().Be(9);
            detail.Balance.Should().Be(950m);
            detail.AllowedActions.Should().Equal("AddExpense", "AddIncome", "TransferTo", "TransferFrom", "PayRecurringBill", "Reconcile");
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetAccount")
                .Which.Attributes[TelemetryAttributeKeys.EntityId].Should().Be(_barclays.Id.ToString());
        }
    }

    [Fact]
    public void GetAccount_UnknownId_ThrowsKeyNotFoundAndFailsTheSpan()
    {
        var act = () => _sut.GetAccount(Guid.NewGuid(), 2026, 10);

        act.Should().Throw<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountQueryService.GetAccount");
        _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void GetAccount_InvalidMonth_ThrowsArgumentOutOfRange(int month)
    {
        var act = () => _sut.GetAccount(_barclays.Id, 2026, month);

        act.Should().Throw<ArgumentOutOfRangeException>();
        _tracer.ShouldHaveFailedSpan<ArgumentOutOfRangeException>("CashFlow.AccountQueryService.GetAccount");
    }

    [Fact]
    public void GetAccounts_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetAccounts();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountQueryService.GetAccounts");
        _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public void GetTransactions_EveryRecordKind_ListsSignedRowsNewestFirst()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetTransactions(_barclays.Id, 2026, 10);

        using (new AssertionScope())
        {
            rows.Select(row => (row.Kind, row.Date.Day, row.Amount, row.RoundUp)).Should().Equal(
                ("Adjustment", 25, 15m, (decimal?)null),
                ("CardCharge", 20, -30m, null),
                ("Income", 14, 300m, null),
                ("TransferIn", 14, 40m, null),
                ("Expense", 10, -42.10m, -0.90m),
                ("TransferOut", 8, -200m, null),
                ("Expense", 1, -5m, null));
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetAccountTransactions");
        }
    }

    [Fact]
    public void GetTransactions_RowText_CarriesDescriptionCategoryAndCounterpart()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetTransactions(_barclays.Id, 2026, 10);

        using (new AssertionScope())
        {
            Row(rows, "Expense", 10).Description.Should().Be("Tesco");
            Row(rows, "Expense", 10).CategoryName.Should().Be("Mercado");
            Row(rows, "Expense", 10).Counterpart.Should().BeNull();
            Row(rows, "CardCharge", 20).Counterpart.Should().Be("Amex");
            Row(rows, "CardCharge", 20).CategoryName.Should().Be("Mercado");
            Row(rows, "Income", 14).Counterpart.Should().Be("Salary");
            Row(rows, "Income", 14).Description.Should().Be("October pay");
            Row(rows, "Income", 14).CategoryName.Should().BeNull();
            Row(rows, "TransferOut", 8).Counterpart.Should().Be("chase");
            Row(rows, "TransferOut", 8).Description.Should().Be("Savings");
            Row(rows, "TransferIn", 14).Counterpart.Should().Be("chase");
            Row(rows, "Adjustment", 25).Description.Should().Be("Statement");
        }
    }

    [Fact]
    public void GetTransactions_EveryRow_FillsExactlyItsOwnSourceRecord()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetTransactions(_barclays.Id, 2026, 10);

        using (new AssertionScope())
        {
            foreach (var row in rows)
                new object?[] { row.Expense, row.Income, row.Transfer, row.Adjustment }.Count(slot => slot is not null).Should().Be(1, row.Kind);

            Row(rows, "Expense", 10).Expense!.Id.Should().Be(Row(rows, "Expense", 10).Id);
            Row(rows, "CardCharge", 20).Expense!.Description.Should().Be("Taxi");
            Row(rows, "Income", 14).Income!.NetValue.Should().Be(300m);
            Row(rows, "TransferOut", 8).Transfer!.DestinationAccountName.Should().Be("chase");
            Row(rows, "Adjustment", 25).Adjustment!.Delta.Should().Be(15m);
        }
    }

    [Fact]
    public void GetTransactions_CardCharge_IsNotEditableButOtherRowsAre()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetTransactions(_barclays.Id, 2026, 10);

        using (new AssertionScope())
        {
            Row(rows, "CardCharge", 20).CanEdit.Should().BeFalse();
            rows.Where(row => row.Kind != "CardCharge").Should().OnlyContain(row => row.CanEdit);
        }
    }

    [Fact]
    public void GetTransactions_InactiveAccount_ListsRowsThatCannotBeEdited()
    {
        var closing = TestAccounts.Bank("Closing");
        _repository.Accounts.Add(closing);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 3), closing, 0m, 0m, null));
        closing.SetActive(false);

        var rows = _sut.GetTransactions(closing.Id, 2026, 10);

        rows.Should().ContainSingle().Which.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void GetTransactions_ZeroDeltaAdjustment_IsListed()
    {
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 3), _chase, 200m, 0m, "Checked"));

        var rows = _sut.GetTransactions(_chase.Id, 2026, 10);

        using (new AssertionScope())
        {
            rows.Should().ContainSingle();
            rows[0].Kind.Should().Be("Adjustment");
            rows[0].Amount.Should().Be(0m);
        }
    }

    [Fact]
    public void GetTransactions_OtherMonthsAndAccounts_AreLeftOut()
    {
        SeedEveryRecordKind();
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 11, 1), "Next month", 9m, _groceries, _barclays, creditCard: null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Chase only", 9m, _groceries, _chase, creditCard: null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Unsettled card", 9m, _groceries, null, _amex));

        var rows = _sut.GetTransactions(_barclays.Id, 2026, 10);

        rows.Select(row => row.Description).Should().NotContain(["Next month", "Chase only", "Unsettled card", "September"]);
    }

    [Fact]
    public void GetTransactions_Transfer_ShowsOnBothAccountsWithOppositeSigns()
    {
        SeedEveryRecordKind();

        var chaseRows = _sut.GetTransactions(_chase.Id, 2026, 10);

        using (new AssertionScope())
        {
            Row(chaseRows, "TransferIn", 8).Amount.Should().Be(200m);
            Row(chaseRows, "TransferIn", 8).Counterpart.Should().Be("Barclays");
            Row(chaseRows, "TransferOut", 14).Amount.Should().Be(-40m);
        }
    }

    [Fact]
    public void GetTransactions_UnknownId_ThrowsKeyNotFoundAndFailsTheSpan()
    {
        var act = () => _sut.GetTransactions(Guid.NewGuid(), 2026, 10);

        act.Should().Throw<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountQueryService.GetAccountTransactions");
    }

    [Fact]
    public void GetTransactions_InvalidMonth_ThrowsArgumentOutOfRange()
    {
        var act = () => _sut.GetTransactions(_barclays.Id, 2026, 13);

        act.Should().Throw<ArgumentOutOfRangeException>();
        _tracer.ShouldHaveFailedSpan<ArgumentOutOfRangeException>("CashFlow.AccountQueryService.GetAccountTransactions");
    }

    private void SeedEveryRecordKind()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 30), "September", 7m, _groceries, _barclays, creditCard: null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 1), "Corner shop", 5m, _groceries, _barclays, creditCard: null));
        var tesco = Expense.Create(new DateOnly(2026, 10, 10), "Tesco", 42.10m, _groceries, _barclays, creditCard: null);
        tesco.SetRoundUpAmount(0.90m);
        _repository.Expenses.Add(tesco);
        var taxi = Expense.Create(new DateOnly(2026, 9, 28), "Taxi", 30m, _groceries, null, _amex);
        taxi.Settle(_barclays, new DateOnly(2026, 10, 20));
        _repository.Expenses.Add(taxi);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 10, 14), _salary, null, 300m, _barclays, "October pay"));
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 10, 8), _barclays, _chase, 200m, null, "Savings", Today));
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 10, 14), _chase, _barclays, 40m, null, null, Today));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 25), _barclays, 0m, 15m, "Statement"));
    }

    private static AccountTransactionDTO Row(IEnumerable<AccountTransactionDTO> rows, string kind, int day) =>
        rows.Single(row => row.Kind == kind && row.Date.Day == day);

    [Fact]
    public void GetAccounts_FillsOpeningBalanceRoundUpAndHasRecords()
    {
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 3), _chase, 200m, 0m, "Checked"));

        var accounts = _sut.GetAccounts().ToDictionary(account => account.Name);

        using (new AssertionScope())
        {
            accounts["chase"].HasRecords.Should().BeTrue();
            accounts["Barclays"].HasRecords.Should().BeFalse();
            accounts["Barclays"].OpeningBalance.Should().Be(1000m);
            accounts["Barclays"].BalanceStartDate.Should().Be(StartDate);
            accounts["Barclays"].RoundUpEnabled.Should().BeTrue();
            accounts["chase"].RoundUpEnabled.Should().BeFalse();
        }
    }

    [Fact]
    public void GetAccounts_TemplateWithoutOpeningBalanceOrRoundUp_LeavesThemNull()
    {
        _repository.Accounts.Add(Account.CreateWithCapabilities("Wallet", Currency.GBP, new HashSet<Capability>()));

        var wallet = _sut.GetAccounts().Single(account => account.Name == "Wallet");

        using (new AssertionScope())
        {
            wallet.OpeningBalance.Should().BeNull();
            wallet.BalanceStartDate.Should().BeNull();
            wallet.RoundUpEnabled.Should().BeNull();
        }
    }
}
