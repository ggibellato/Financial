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
        _sut = new AccountQueryService(_repository, TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)), _tracer, _logger);
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
            detail.BalanceIsOwed.Should().BeFalse();
            detail.HasStatements.Should().BeFalse();
            detail.TotalOwed.Should().BeNull();
            detail.AllowedActions.Should().Equal("AddExpense", "AddIncome", "TransferTo", "TransferFrom", "PayRecurringBill", "PayCardStatement", "ReceiveCardCredit", "Reconcile", "WithdrawFromPots");
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

    private (Account Pots, Pot Pot) AddPotsAccount(bool isActive = true)
    {
        var pots = TestAccounts.Pots("Holiday pots", isActive);
        var pot = Pot.Create("Trips", 100m);
        pots.AddPot(pot);
        _repository.Accounts.Add(pots);
        return (pots, pot);
    }

    private Income AddPotsIncome(Account pots)
    {
        var income = Income.Create(
            new DateOnly(2026, 10, 14), IncomeSource.Create("Ramsay", IncomeGroup.Salary, potsAccount: pots), null, 100m, _barclays, null, pots);
        _repository.Incomes.Add(income);
        return income;
    }

    [Fact]
    public void GetTransactions_PotsAccount_ListsOnlyItsOwnMovementsInTheMonth()
    {
        var (pots, pot) = AddPotsAccount();
        var (_, otherPot) = AddPotsAccount();
        var legacy = PotTransaction.Create(pot, -12.40m, new DateOnly(2026, 10, 5), "Boots");
        _repository.PotTransactions.AddRange([
            legacy,
            PotTransaction.Create(pot, 50m, new DateOnly(2026, 9, 30), "Earlier"),
            PotTransaction.Create(otherPot, 7m, new DateOnly(2026, 10, 6), "Other account")]);

        var row = _sut.GetTransactions(pots.Id, 2026, 10).Should().ContainSingle().Which;

        using (new AssertionScope())
        {
            row.Id.Should().Be(legacy.Id);
            row.Kind.Should().Be("PotTransaction");
            row.Date.Should().Be(new DateOnly(2026, 10, 5));
            row.Counterpart.Should().Be("Trips");
            row.Description.Should().Be("Boots");
            row.Amount.Should().Be(-12.40m);
            row.CanEdit.Should().BeTrue();
            row.Operation.Should().BeNull();
            row.Movement!.Id.Should().Be(legacy.Id);
            row.Movement.BucketId.Should().Be(pot.Id);
            row.Expense.Should().BeNull();
        }
    }

    [Fact]
    public void GetTransactions_PotsAccount_MovementCarriesTheLinesItsDeleteRemoves()
    {
        var (pots, pot) = AddPotsAccount();
        var date = new DateOnly(2026, 10, 5);
        var line = PotTransaction.Create(pot, 10m, date, "Bonus");
        var withdrawal = PotTransaction.Create(pot, -5m, date, "Bonus");
        _repository.PotTransactions.AddRange([line, PotTransaction.Create(pot, 20m, date, "Bonus"), withdrawal]);

        var rows = _sut.GetTransactions(pots.Id, 2026, 10);

        using (new AssertionScope())
        {
            rows.Single(row => row.Id == line.Id).Movement!.SplitLineCount.Should().Be(2);
            rows.Single(row => row.Id == withdrawal.Id).Movement!.SplitLineCount.Should().Be(1);
        }
    }

    [Fact]
    public void GetTransactions_InactivePotsAccount_MovementCannotBeEdited()
    {
        var (pots, pot) = AddPotsAccount(isActive: false);
        _repository.PotTransactions.Add(PotTransaction.Create(pot, 10m, new DateOnly(2026, 10, 5), "Boots"));

        _sut.GetTransactions(pots.Id, 2026, 10).Should().ContainSingle().Which.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void GetTransactions_LinkedRecords_CarryTheOperationAndCannotBeEdited()
    {
        var (pots, pot) = AddPotsAccount();
        var operationId = Guid.NewGuid();
        _repository.PotTransactions.Add(PotTransaction.Create(pot, -30m, new DateOnly(2026, 10, 5), "Boots", operationId: operationId));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 10, 5), "Boots", 30m, _groceries, _barclays, creditCard: null, operationId: operationId));

        var rows = _sut.GetTransactions(pots.Id, 2026, 10).Concat(_sut.GetTransactions(_barclays.Id, 2026, 10)).ToList();

        using (new AssertionScope())
        {
            rows.Should().HaveCount(2);
            rows.Should().OnlyContain(row => !row.CanEdit);
            rows.Select(row => row.Operation).Should().AllSatisfy(link =>
            {
                link!.OperationId.Should().Be(operationId);
                link.PotsAccountId.Should().Be(pots.Id);
                link.Year.Should().Be(2026);
                link.Month.Should().Be(10);
                link.IncomeId.Should().BeNull();
            });
        }
    }

    [Fact]
    public void GetTransactions_IncomeWithLinkedDeposit_RowCarriesItsFundingLines()
    {
        var (pots, pot) = AddPotsAccount();
        var income = AddPotsIncome(pots);
        var operationId = Guid.NewGuid();
        _repository.PotTransactions.Add(PotTransaction.Create(pot, 90m, income.Date, "Pay", income, operationId));
        _repository.Expenses.Add(Expense.Create(income.Date, "Deposit", 90m, _groceries, _barclays, creditCard: null, operationId: operationId));

        var bankRows = _sut.GetTransactions(_barclays.Id, 2026, 10);
        var incomeRow = Row(bankRows, "Income", 14);

        using (new AssertionScope())
        {
            incomeRow.CanEdit.Should().BeTrue();
            incomeRow.Income!.HasLinkedDeposit.Should().BeTrue();
            incomeRow.Income.FundingLines.Select(line => (line.BankId, line.Amount)).Should().Equal((_barclays.Id, 90m));
            Row(bankRows, "Expense", 14).Operation!.IncomeId.Should().Be(income.Id);
            _sut.GetTransactions(pots.Id, 2026, 10).Single().Operation!.IncomeId.Should().Be(income.Id);
        }
    }

    [Fact]
    public void GetTransactions_OlderSplitIncome_MovementCannotBeEditedAndIncomeHasNoDeposit()
    {
        var (pots, pot) = AddPotsAccount();
        var income = AddPotsIncome(pots);
        _repository.PotTransactions.Add(PotTransaction.Create(pot, 90m, income.Date, "Pay", income));

        var movementRow = _sut.GetTransactions(pots.Id, 2026, 10).Single();
        var incomeRow = _sut.GetTransactions(_barclays.Id, 2026, 10).Single();

        using (new AssertionScope())
        {
            movementRow.CanEdit.Should().BeFalse();
            movementRow.Operation.Should().BeNull();
            incomeRow.Income!.HasLinkedDeposit.Should().BeFalse();
            incomeRow.Income.FundingLines.Should().BeEmpty();
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

    [Fact]
    public void GetTransactions_CardAccount_ListsEveryChargeOfTheStatementMonthWithItsPurchaseDate()
    {
        _repository.Accounts.Add(_amex);
        var taxi = Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex);
        var settled = Expense.Create(new DateOnly(2026, 10, 14), "Settled", 20m, _groceries, null, _amex);
        settled.Settle(_barclays, new DateOnly(2026, 11, 5));
        var lateSeptember = Expense.Create(new DateOnly(2026, 9, 28), "Late September", 7m, _groceries, null, _amex, new DateOnly(2026, 10, 1));
        _repository.Expenses.AddRange([taxi, settled, lateSeptember, Expense.Create(new DateOnly(2026, 9, 12), "September", 9m, _groceries, null, _amex)]);

        var rows = _sut.GetTransactions(_amex.Id, 2026, 10);

        using (new AssertionScope())
        {
            rows.Select(row => row.Id).Should().Equal(settled.Id, taxi.Id, lateSeptember.Id);
            rows.Select(row => row.Date).Should().Equal(new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 12), new DateOnly(2026, 9, 28));
            rows.Should().OnlyContain(row => row.Kind == "CardCharge" && row.CategoryName == "Mercado");
            rows.Select(row => row.Counterpart).Should().Equal("Barclays", null, null);
            rows.Select(row => row.Amount).Should().Equal(-20m, -30m, -7m);
            rows.Select(row => row.CanEdit).Should().Equal(false, true, true);
        }
    }

    [Fact]
    public void GetTransactions_NegativeCardCharge_IsACardRefundWithAPositiveAmount()
    {
        _repository.Accounts.Add(_amex);
        var charge = Expense.Create(new DateOnly(2026, 10, 3), "Shoes", 70m, _groceries, null, _amex);
        var refund = Expense.Create(new DateOnly(2026, 10, 8), "Shoes returned", -40m, _groceries, null, _amex);
        _repository.Expenses.AddRange([charge, refund]);

        var rows = _sut.GetTransactions(_amex.Id, 2026, 10);

        using (new AssertionScope())
        {
            var refundRow = rows.Single(row => row.Id == refund.Id);
            refundRow.Kind.Should().Be("CardRefund");
            refundRow.Amount.Should().Be(40m);
            refundRow.CanEdit.Should().BeTrue();
            refundRow.Expense!.Value.Should().Be(-40m);
            rows.Single(row => row.Id == charge.Id).Kind.Should().Be("CardCharge");
        }
    }

    private void AddSettledRefund()
    {
        _repository.Accounts.Add(_amex);
        var refund = Expense.Create(new DateOnly(2026, 10, 8), "Shoes returned", -40m, _groceries, null, _amex);
        _repository.Expenses.Add(refund);
        refund.Settle(_barclays, new DateOnly(2026, 10, 20));
    }

    [Fact]
    public void GetTransactions_RefundOnAPaidStatement_IsACardRefundThatCannotBeEdited()
    {
        AddSettledRefund();

        var row = _sut.GetTransactions(_amex.Id, 2026, 10).Single();

        using (new AssertionScope())
        {
            row.Kind.Should().Be("CardRefund");
            row.Amount.Should().Be(40m);
            row.CanEdit.Should().BeFalse();
        }
    }

    [Fact]
    public void GetTransactions_SettledRefundOnItsBank_IsACardRefundNamingTheCard()
    {
        AddSettledRefund();

        var row = _sut.GetTransactions(_barclays.Id, 2026, 10).Single();

        using (new AssertionScope())
        {
            row.Kind.Should().Be("CardRefund");
            row.Amount.Should().Be(40m);
            row.Counterpart.Should().Be("Amex");
            row.CanEdit.Should().BeFalse();
        }
    }

    [Fact]
    public void GetTransactions_CardStatementPaid_ChargesAreReadOnly()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex));
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 10).MarkLegacyPaid());

        _sut.GetTransactions(_amex.Id, 2026, 10).Should().ContainSingle().Which.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void GetTransactions_InactiveCard_ChargesAreReadOnly()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex));
        _amex.SetActive(false);

        _sut.GetTransactions(_amex.Id, 2026, 10).Should().ContainSingle().Which.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void GetTransactions_CardWithAPaymentInTheMonth_ListsItAsAReadOnlyCardPaymentNamingItsStatement()
    {
        _repository.Accounts.Add(_amex);
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 10, 20), _barclays, _amex, 40m, Today);
        _repository.Transfers.Add(payment);
        var september = CardStatement.Create(_amex, 2026, 9);
        september.MarkPaidBy(payment.Id);
        _repository.CardStatements.Add(september);

        var row = _sut.GetTransactions(_amex.Id, 2026, 10).Should().ContainSingle().Which;

        using (new AssertionScope())
        {
            row.Id.Should().Be(payment.Id);
            row.Kind.Should().Be("CardPayment");
            row.Description.Should().Be("Amex payment – Sep 2026 statement");
            row.Counterpart.Should().Be("Barclays");
            row.Amount.Should().Be(40m);
            row.CanEdit.Should().BeFalse();
            row.Statement.Should().BeEquivalentTo(new { AccountId = _amex.Id, Year = 2026, Month = 9 });
        }
    }

    [Fact]
    public void GetStatements_BankAccount_ThrowsNotACard()
    {
        var act = () => _sut.GetStatements(_barclays.Id);

        act.Should().Throw<ArgumentException>().WithMessage("Barclays is not a card account.");
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.AccountQueryService.GetAccountStatements");
    }

    [Fact]
    public void GetStatements_UnknownId_ThrowsKeyNotFoundAndFailsTheSpan()
    {
        var act = () => _sut.GetStatements(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountQueryService.GetAccountStatements");
    }

    [Fact]
    public void GetStatements_Card_ListsEveryLedgerMonthNewestFirst_WithIdsOnlyOnStoredStatements()
    {
        _repository.Accounts.Add(_amex);
        var august = CardStatement.Create(_amex, 2026, 8);
        var september = CardStatement.Create(_amex, 2026, 9);
        september.SetDueDate(new DateOnly(2026, 10, 3));
        var november = CardStatement.Create(_amex, 2026, 11);
        _repository.CardStatements.AddRange([august, september, november, CardStatement.Create(TestAccounts.CreditCard("Visa"), 2026, 10)]);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 9, 2), "Shop", 50m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 9, 9), "Fuel", 25m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 11, 3), "Taxi", 12m, _groceries, null, _amex)]);
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 10, 12), _barclays, _amex, 75m, Today);
        _repository.Transfers.Add(payment);
        september.MarkPaidBy(payment.Id);
        august.MarkLegacyPaid();

        var rows = _sut.GetStatements(_amex.Id);

        using (new AssertionScope())
        {
            rows.Select(row => (row.Year, row.Month)).Should().Equal((2026, 11), (2026, 10), (2026, 9));
            rows[0].Should().BeEquivalentTo(new { Id = (Guid?)november.Id, Total = 12m, CreditIn = 0m, OutstandingTotal = 12m, CreditCarried = 0m, Status = "Unpaid", IsPaid = false, DueDate = (DateOnly?)null, PaidOn = (DateOnly?)null, PaidAmount = (decimal?)null, PaidFromAccountName = (string?)null });
            rows[1].Should().BeEquivalentTo(new { Id = (Guid?)null, Total = 0m, OutstandingTotal = 0m, CreditCarried = 0m, Status = "NothingDue", IsPaid = false, DueDate = (DateOnly?)null, PaidOn = (DateOnly?)null });
            rows[2].Should().BeEquivalentTo(new { Id = (Guid?)september.Id, Total = 75m, OutstandingTotal = 0m, Status = "Paid", IsPaid = true, DueDate = new DateOnly(2026, 10, 3), PaidOn = new DateOnly(2026, 10, 12), PaidAmount = 75m, PaidFromAccountName = "Barclays" });
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetAccountStatements");
        }
    }

    [Fact]
    public void GetStatements_CardWithoutCharges_IsEmpty()
    {
        _repository.Accounts.Add(_amex);
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 10));

        _sut.GetStatements(_amex.Id).Should().BeEmpty();
    }

    [Fact]
    public void GetStatements_RefundAboveItsCharges_CarriesCreditIntoTheNextMonth()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 10, 2), "Shop", 25m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 9), "Shop refund", -60m, _groceries, null, _amex)]);

        var rows = _sut.GetStatements(_amex.Id);

        using (new AssertionScope())
        {
            rows[0].Should().BeEquivalentTo(new { Month = 11, Total = 0m, CreditIn = 35m, OutstandingTotal = 0m, CreditCarried = 35m, Status = "NothingDue" });
            rows[1].Should().BeEquivalentTo(new { Month = 10, Total = -35m, CreditIn = 0m, OutstandingTotal = 0m, CreditCarried = 35m, Status = "NothingDue" });
        }
    }

    [Fact]
    public void GetAccount_Card_ViewedStatementIsTheViewedMonthsLedgerRow_AndBalanceAndTotalOwedAreUnchanged()
    {
        _repository.Accounts.Add(_amex);
        var november = CardStatement.Create(_amex, 2026, 11);
        november.SetDueDate(new DateOnly(2026, 11, 28));
        _repository.CardStatements.Add(november);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 11, 2), "Bought in November", 300m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 12, 2), "Next statement", 80m, _groceries, null, _amex)]);

        var detail = _sut.GetAccount(_amex.Id, 2026, 11);

        using (new AssertionScope())
        {
            detail.ViewedStatement.Should().BeEquivalentTo(new { Id = (Guid?)november.Id, Year = 2026, Month = 11, Total = 300m, OutstandingTotal = 300m, Status = "Unpaid", DueDate = new DateOnly(2026, 11, 28) });
            detail.Balance.Should().Be(-300m);
            detail.TotalOwed.Should().Be(380m);
        }
    }

    [Fact]
    public void GetAccount_CardFutureMonth_ViewedStatementReachesThatMonthWithTheCarriedCredit()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 2), "Refund", -35m, _groceries, null, _amex));

        var detail = _sut.GetAccount(_amex.Id, 2027, 3);

        detail.ViewedStatement.Should().BeEquivalentTo(new { Id = (Guid?)null, Year = 2027, Month = 3, CreditIn = 35m, CreditCarried = 35m, Status = "NothingDue" });
    }

    [Fact]
    public void GetAccount_CardMonthBeforeItsFirstCharge_HasNoViewedStatement()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 2), "Taxi", 30m, _groceries, null, _amex));

        _sut.GetAccount(_amex.Id, 2026, 9).ViewedStatement.Should().BeNull();
    }

    [Fact]
    public void GetAccount_Bank_HasNoViewedStatement()
    {
        _sut.GetAccount(_barclays.Id, 2026, 10).ViewedStatement.Should().BeNull();
    }

    [Fact]
    public void GetStatements_LegacyPaidStatement_ReportsTheLatestSettlementAndItsBank()
    {
        _repository.Accounts.Add(_amex);
        var early = Expense.Create(new DateOnly(2026, 10, 2), "Shop", 50m, _groceries, null, _amex);
        var late = Expense.Create(new DateOnly(2026, 10, 9), "Fuel", 25m, _groceries, null, _amex);
        early.Settle(_chase, new DateOnly(2026, 11, 1));
        late.Settle(_barclays, new DateOnly(2026, 11, 4));
        _repository.Expenses.AddRange([early, late]);
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 10).MarkLegacyPaid());

        var row = _sut.GetStatements(_amex.Id).Single(statement => statement.Month == 10);

        using (new AssertionScope())
        {
            row.Total.Should().Be(75m);
            row.OutstandingTotal.Should().Be(0m);
            row.PaidAmount.Should().Be(75m);
            row.PaidOn.Should().Be(new DateOnly(2026, 11, 4));
            row.PaidFromAccountName.Should().Be("Barclays");
        }
    }

    [Fact]
    public void GetAccount_CardAccount_OffersAddChargeAndOwesItsUnpaidCharges()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 9, 12), "September", 9m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 11, 1), "November", 50m, _groceries, null, _amex)]);

        var detail = _sut.GetAccount(_amex.Id, 2026, 10);

        using (new AssertionScope())
        {
            detail.AllowedActions.Should().Equal("AddCharge", "AddRefund");
            detail.Balance.Should().Be(-39m);
            detail.BalanceIsOwed.Should().BeTrue();
            detail.HasStatements.Should().BeTrue();
            detail.TemplateName.Should().Be("CreditCard");
        }
    }

    [Fact]
    public void GetAccount_CardInTwoMonths_TotalOwedCountsEveryUnpaidChargeAndDoesNotChange()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 9, 12), "September", 9m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 11, 1), "November", 50m, _groceries, null, _amex)]);
        _repository.Transfers.Add(Transfer.CreateStatementPayment(new DateOnly(2026, 10, 20), _barclays, _amex, 9m, Today));

        var september = _sut.GetAccount(_amex.Id, 2026, 9);
        var october = _sut.GetAccount(_amex.Id, 2026, 10);

        using (new AssertionScope())
        {
            september.Balance.Should().Be(-9m);
            october.Balance.Should().Be(-30m);
            september.TotalOwed.Should().Be(80m);
            october.TotalOwed.Should().Be(80m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-02")]
    public void GetTransactions_BankWithAStatementPayment_ListsOneReadOnlyCardPaymentRowLinkedToItsStatement()
    {
        _repository.Accounts.Add(_amex);
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 11, 12), _barclays, _amex, 30m, Today);
        _repository.Transfers.Add(payment);
        var october = CardStatement.Create(_amex, 2026, 10);
        october.MarkPaidBy(payment.Id);
        _repository.CardStatements.AddRange([october, CardStatement.Create(_amex, 2026, 11)]);

        var row = _sut.GetTransactions(_barclays.Id, 2026, 11).Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            row.Id.Should().Be(payment.Id);
            row.Kind.Should().Be("CardPayment");
            row.Description.Should().Be("Amex payment – Oct 2026 statement");
            row.Counterpart.Should().Be("Amex");
            row.Amount.Should().Be(-30m);
            row.CanEdit.Should().BeFalse();
            row.Statement.Should().BeEquivalentTo(new { AccountId = _amex.Id, Year = 2026, Month = 10 });
        }
    }

    [Fact]
    public void GetTransactions_CreditRefund_IsAReadOnlyRowOnBothAccountsWithTheSignOfEachSide()
    {
        _repository.Accounts.Add(_amex);
        var refund = Transfer.CreateCreditRefund(new DateOnly(2026, 10, 20), _amex, _barclays, 35m, null, Today);
        _repository.Transfers.Add(refund);

        var bankRow = _sut.GetTransactions(_barclays.Id, 2026, 10).Should().ContainSingle().Subject;
        var cardRow = _sut.GetTransactions(_amex.Id, 2026, 10).Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            bankRow.Should().BeEquivalentTo(new { refund.Id, Kind = "CardCreditRefund", Description = "Amex credit refund", Counterpart = "Amex", Amount = 35m, CanEdit = false });
            bankRow.Transfer!.Id.Should().Be(refund.Id);
            cardRow.Should().BeEquivalentTo(new { refund.Id, Kind = "CardCreditRefund", Description = "Amex credit refund", Counterpart = "Barclays", Amount = -35m, CanEdit = false });
            cardRow.Transfer!.Id.Should().Be(refund.Id);
        }
    }

    [Fact]
    public void GetTransactions_CreditRefundWithANote_ShowsTheNote()
    {
        _repository.Accounts.Add(_amex);
        _repository.Transfers.Add(Transfer.CreateCreditRefund(new DateOnly(2026, 10, 20), _amex, _barclays, 35m, "Issuer cheque", Today));

        _sut.GetTransactions(_barclays.Id, 2026, 10).Should().ContainSingle().Which.Description.Should().Be("Issuer cheque");
    }

    [Theory]
    [InlineData(-60, true)]
    [InlineData(0, false)]
    [InlineData(25, false)]
    public void GetAccount_Card_OffersRefundCreditToBankOnlyWhenTotalOwedIsBelowZero(int chargeValue, bool isOffered)
    {
        _repository.Accounts.Add(_amex);
        if (chargeValue != 0)
            _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Charge", chargeValue, _groceries, null, _amex));

        var actions = _sut.GetAccount(_amex.Id, 2026, 10).AllowedActions;

        actions.Contains("RefundCreditToBank").Should().Be(isOffered);
    }

    [Fact]
    public void GetAccount_InactiveCardInCredit_OffersNoActions()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Refund", -60m, _groceries, null, _amex));
        _amex.SetActive(false);

        _sut.GetAccount(_amex.Id, 2026, 10).AllowedActions.Should().BeEmpty();
    }

    [Fact]
    public void GetAccount_Bank_DoesNotOfferRefundCreditToBank()
    {
        _sut.GetAccount(_barclays.Id, 2026, 10).AllowedActions.Should().NotContain("RefundCreditToBank");
    }

    [Fact]
    public void GetTransactions_StatementPaymentNoStatementLinks_FallsBackToTheTransferNoteWithNoStatementOnEitherSide()
    {
        _repository.Accounts.Add(_amex);
        _repository.Transfers.Add(Transfer.CreateStatementPayment(new DateOnly(2026, 10, 20), _barclays, _amex, 30m, Today));
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 9).MarkLegacyPaid());

        var bankRow = _sut.GetTransactions(_barclays.Id, 2026, 10).Should().ContainSingle().Subject;
        var cardRow = _sut.GetTransactions(_amex.Id, 2026, 10).Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            bankRow.Kind.Should().Be("CardPayment");
            bankRow.Description.Should().BeNull();
            bankRow.Statement.Should().BeNull();
            bankRow.CanEdit.Should().BeFalse();
            cardRow.Kind.Should().Be("CardPayment");
            cardRow.Description.Should().BeNull();
            cardRow.Statement.Should().BeNull();
        }
    }

    [Fact]
    public void GetTransactions_OrdinaryTransfer_KeepsItsNoteAndHasNoStatement()
    {
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 10, 8), _barclays, _chase, 25m, null, "Savings", Today));

        var row = _sut.GetTransactions(_barclays.Id, 2026, 10).Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            row.Kind.Should().Be("TransferOut");
            row.Description.Should().Be("Savings");
            row.Statement.Should().BeNull();
        }
    }

    [Fact]
    public void GetPayableStatements_ListsUnpaidStatementsWithSomethingToPay_InactiveCardsIncluded_ByCardThenMonth()
    {
        var visa = TestAccounts.CreditCard("visa");
        var closed = TestAccounts.CreditCard("Closed card");
        _repository.Accounts.AddRange([_amex, visa, closed]);
        var amexNovember = CardStatement.Create(_amex, 2026, 11);
        var amexOctober = CardStatement.Create(_amex, 2026, 10);
        var visaOctober = CardStatement.Create(visa, 2026, 10);
        var closedOctober = CardStatement.Create(closed, 2026, 10);
        _repository.CardStatements.AddRange([
            amexNovember,
            amexOctober,
            CardStatement.Create(_amex, 2026, 9).MarkLegacyPaid(),
            CardStatement.Create(_amex, 2026, 8),
            CardStatement.Create(_amex, 2026, 7),
            visaOctober,
            closedOctober]);
        var settled = Expense.Create(new DateOnly(2026, 7, 2), "Settled", 5m, _groceries, null, _amex);
        settled.Settle(_barclays, new DateOnly(2026, 8, 1));
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 11, 2), "Taxi", 12m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 2), "Shop", 50m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 9), "Fuel", 25m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 9, 9), "Paid", 7m, _groceries, null, _amex),
            settled,
            Expense.Create(new DateOnly(2026, 10, 4), "Books", 20m, _groceries, null, visa),
            Expense.Create(new DateOnly(2026, 10, 5), "Old", 3m, _groceries, null, closed)]);
        closed.SetActive(false);

        var rows = _sut.GetPayableStatements();

        using (new AssertionScope())
        {
            rows.Select(row => (row.Id, row.CreditCardName, row.Year, row.Month, row.Total)).Should().Equal(
                (amexOctober.Id, "Amex", 2026, 10, 75m),
                (amexNovember.Id, "Amex", 2026, 11, 12m),
                (closedOctober.Id, "Closed card", 2026, 10, 3m),
                (visaOctober.Id, "visa", 2026, 10, 20m));
            rows[0].CreditCardId.Should().Be(_amex.Id);
            rows[0].Currency.Should().Be("GBP");
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetPayableStatements");
        }
    }

    [Fact]
    public void GetPayableStatements_CarriedCredit_LowersTheAmountAndNothingDueMonthsAreLeftOut()
    {
        _repository.Accounts.Add(_amex);
        var october = CardStatement.Create(_amex, 2026, 10);
        var november = CardStatement.Create(_amex, 2026, 11);
        _repository.CardStatements.AddRange([october, november]);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 10, 9), "Refund", -35m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 11, 2), "Taxi", 50m, _groceries, null, _amex)]);

        var row = _sut.GetPayableStatements().Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new { Id = november.Id, Year = 2026, Month = 11, Total = 15m });
    }

    [Fact]
    public void GetPayableStatements_UnpaidMonthWithoutAStoredStatement_IsLeftOut()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 11, 2), "Taxi", 50m, _groceries, null, _amex));

        _sut.GetPayableStatements().Should().BeEmpty();
    }

    [Fact]
    public void GetPayableStatements_RepositoryReadFails_FailsTheSpan()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetPayableStatements();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountQueryService.GetPayableStatements");
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-02")]
    public void GetAccount_CardAfterAStatementPayment_OwesItsChargesLessThePayment()
    {
        _repository.Accounts.Add(_amex);
        _repository.Expenses.AddRange([
            Expense.Create(new DateOnly(2026, 9, 12), "September", 9m, _groceries, null, _amex),
            Expense.Create(new DateOnly(2026, 10, 12), "Taxi", 30m, _groceries, null, _amex)]);
        _repository.Transfers.Add(Transfer.CreateStatementPayment(new DateOnly(2026, 10, 20), _barclays, _amex, 30m, Today));

        using (new AssertionScope())
        {
            _sut.GetAccount(_amex.Id, 2026, 10).Balance.Should().Be(-9m);
            _sut.GetAccount(_barclays.Id, 2026, 10).Balance.Should().Be(970m);
        }
    }

    [Fact]
    public void GetStatements_UnpaidStatementWithASettledCharge_OwesOnlyItsUnsettledCharges()
    {
        _repository.Accounts.Add(_amex);
        var settled = Expense.Create(new DateOnly(2026, 10, 2), "Shop", 50m, _groceries, null, _amex);
        settled.Settle(_barclays, new DateOnly(2026, 11, 1));
        _repository.Expenses.AddRange([settled, Expense.Create(new DateOnly(2026, 10, 9), "Fuel", 25m, _groceries, null, _amex)]);
        _repository.CardStatements.Add(CardStatement.Create(_amex, 2026, 10));

        var row = _sut.GetStatements(_amex.Id).Single(statement => statement.Month == 10);

        using (new AssertionScope())
        {
            row.Total.Should().Be(75m);
            row.OutstandingTotal.Should().Be(25m);
        }
    }

    [Fact]
    public void GetAccount_PotsAccount_BalanceIsItsPotsMovementsAndItOffersThePotActions()
    {
        var reserva = _repository.EnsureReserva();
        var pot = Pot.Create("Investimento", 100m);
        reserva.AddPot(pot);
        _repository.PotTransactions.AddRange([
            PotTransaction.Create(pot, 300m, new DateOnly(2025, 6, 1), "Split"),
            PotTransaction.Create(pot, -50m, new DateOnly(2026, 9, 30), "Withdrawal"),
            PotTransaction.Create(pot, 80m, new DateOnly(2026, 10, 1), "Next month")
        ]);

        var detail = _sut.GetAccount(reserva.Id, 2026, 9);

        using (new AssertionScope())
        {
            detail.TemplateName.Should().Be("Pots");
            detail.Balance.Should().Be(250m);
            detail.BalanceIsOwed.Should().BeFalse();
            detail.HasStatements.Should().BeFalse();
            detail.TotalOwed.Should().BeNull();
            detail.AllowedActions.Should().Equal("Deposit", "Withdraw", "ManagePots");
            _sut.GetAccount(_barclays.Id, 2026, 9).Balance.Should().Be(1000m);
        }
    }

    [Fact]
    public void GetTransactions_Bank_DoesNotListPotMovements()
    {
        var reserva = _repository.EnsureReserva();
        var pot = Pot.Create("Investimento", 100m);
        reserva.AddPot(pot);
        _repository.PotTransactions.Add(PotTransaction.Create(pot, 300m, new DateOnly(2026, 9, 5), "Split"));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 9, 5), _salary, null, 300m, _barclays));

        _sut.GetTransactions(_barclays.Id, 2026, 9).Should().ContainSingle().Which.Kind.Should().Be("Income");
    }

    [Fact]
    public void GetAccounts_PotsAccount_HasRecordsOnlyWhenItOwnsPots()
    {
        var reserva = _repository.EnsureReserva();
        reserva.AddPot(Pot.Create("Investimento", 100m));
        _repository.Accounts.Add(TestAccounts.Pots("Holidays"));

        var accounts = _sut.GetAccounts().ToDictionary(account => account.Name);

        using (new AssertionScope())
        {
            accounts["Reserva"].HasRecords.Should().BeTrue();
            accounts["Holidays"].HasRecords.Should().BeFalse();
        }
    }

    [Fact]
    public void GetMonthTransactions_EveryAccount_ListsItsRowsWithItsAccount()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetMonthTransactions(2026, 10);

        using (new AssertionScope())
        {
            rows.Select(row => row.AccountId).Distinct().Should().BeEquivalentTo([_barclays.Id, _chase.Id]);
            rows.Where(row => row.AccountId == _barclays.Id).Should().OnlyContain(row =>
                row.AccountName == "Barclays" && row.AccountCurrency == "GBP" && row.AccountIsActive);
            rows.Where(row => row.AccountId == _chase.Id).Should().OnlyContain(row =>
                row.AccountName == "chase" && row.AccountCurrency == "GBP" && row.AccountIsActive);
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetMonthTransactions")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public void GetMonthTransactions_EqualsEachAccountsTransactions_WithoutTheBankCopyOfSettledCharges()
    {
        _repository.Accounts.Add(_amex);
        SeedEveryRecordKind();
        var (pots, pot) = AddPotsAccount();
        var operationId = Guid.NewGuid();
        _repository.PotTransactions.Add(PotTransaction.Create(pot, 30m, new DateOnly(2026, 10, 5), "Deposit", operationId: operationId));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 10, 5), "Deposit", 30m, _groceries, _barclays, creditCard: null, operationId: operationId));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 10, 12), "Train", 18m, _groceries, null, _amex));
        var settledCharge = _repository.Expenses.Single(expense => expense.Description == "Taxi");

        var rows = _sut.GetMonthTransactions(2026, 10);
        var accountScreens = _repository.Accounts
            .SelectMany(account => _sut.GetTransactions(account.Id, 2026, 10).Select(row => (account.Id, Row: row)))
            .Where(pair => pair.Row.Id != settledCharge.Id);

        using (new AssertionScope())
        {
            rows.Select(row => (row.AccountId, row.Transaction)).Should().BeEquivalentTo(accountScreens);
            rows.Should().Contain(row => row.AccountId == pots.Id);
            rows.Should().NotContain(row => row.Transaction.Id == settledCharge.Id);
        }
    }

    [Fact]
    public void GetMonthTransactions_SettledCardCharge_ListedOnceOnItsCardInItsStatementMonth()
    {
        _repository.Accounts.Add(_amex);
        SeedEveryRecordKind();
        var settledCharge = _repository.Expenses.Single(expense => expense.Description == "Taxi");

        var september = _sut.GetMonthTransactions(2026, 9).Where(row => row.Transaction.Id == settledCharge.Id).ToList();
        var october = _sut.GetMonthTransactions(2026, 10);

        using (new AssertionScope())
        {
            september.Should().ContainSingle();
            september[0].AccountId.Should().Be(_amex.Id);
            september[0].Transaction.Kind.Should().Be("CardCharge");
            september[0].Transaction.Date.Should().Be(new DateOnly(2026, 9, 28));
            october.Should().NotContain(row => row.Transaction.Id == settledCharge.Id);
        }
    }

    [Fact]
    public void GetMonthTransactions_TransferCardPaymentAndCreditRefund_ShowOnceOnEachSide()
    {
        _repository.Accounts.Add(_amex);
        var transfer = Transfer.Create(new DateOnly(2026, 10, 8), _barclays, _chase, 200m, null, "Savings", Today);
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 10, 20), _barclays, _amex, 40m, Today);
        var creditRefund = Transfer.CreateCreditRefund(new DateOnly(2026, 10, 22), _amex, _chase, 5m, null, Today);
        _repository.Transfers.AddRange([transfer, payment, creditRefund]);

        var rows = _sut.GetMonthTransactions(2026, 10);

        using (new AssertionScope())
        {
            rows.Where(row => row.Transaction.Id == transfer.Id).Select(row => (row.AccountId, row.Transaction.Kind, row.Transaction.Amount))
                .Should().BeEquivalentTo([(_barclays.Id, "TransferOut", -200m), (_chase.Id, "TransferIn", 200m)]);
            rows.Where(row => row.Transaction.Id == payment.Id).Select(row => (row.AccountId, row.Transaction.Kind, row.Transaction.Amount))
                .Should().BeEquivalentTo([(_barclays.Id, "CardPayment", -40m), (_amex.Id, "CardPayment", 40m)]);
            rows.Where(row => row.Transaction.Id == creditRefund.Id).Select(row => (row.AccountId, row.Transaction.Kind, row.Transaction.Amount))
                .Should().BeEquivalentTo([(_amex.Id, "CardCreditRefund", -5m), (_chase.Id, "CardCreditRefund", 5m)]);
        }
    }

    [Fact]
    public void GetMonthTransactions_PotDeposit_ListsBankExpensesAndOneRowPerPot()
    {
        var (pots, pot) = AddPotsAccount();
        var otherPot = Pot.Create("Gifts", 0m);
        pots.AddPot(otherPot);
        var operationId = Guid.NewGuid();
        var date = new DateOnly(2026, 10, 5);
        _repository.PotTransactions.AddRange([
            PotTransaction.Create(pot, 60m, date, "Deposit", operationId: operationId),
            PotTransaction.Create(otherPot, 30m, date, "Deposit", operationId: operationId)]);
        _repository.Expenses.AddRange([
            Expense.Create(date, "Deposit", 50m, _groceries, _barclays, creditCard: null, operationId: operationId),
            Expense.Create(date, "Deposit", 40m, _groceries, _chase, creditCard: null, operationId: operationId)]);

        var rows = _sut.GetMonthTransactions(2026, 10);

        using (new AssertionScope())
        {
            rows.Select(row => (row.AccountName, row.Transaction.Kind, row.Transaction.Counterpart, row.Transaction.Amount)).Should().BeEquivalentTo([
                ("Barclays", "Expense", (string?)null, -50m),
                ("chase", "Expense", null, -40m),
                ("Holiday pots", "PotTransaction", "Trips", 60m),
                ("Holiday pots", "PotTransaction", "Gifts", 30m)]);
            rows.Should().OnlyContain(row => row.Transaction.Operation!.OperationId == operationId);
        }
    }

    [Fact]
    public void GetMonthTransactions_InactiveAccount_IsListedAndFlagged()
    {
        var closing = TestAccounts.Bank("Closing");
        _repository.Accounts.Add(closing);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 3), _barclays, 0m, 4m, null));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 10, 3), closing, 0m, 9m, null));
        closing.SetActive(false);

        var rows = _sut.GetMonthTransactions(2026, 10);

        using (new AssertionScope())
        {
            rows.Single(row => row.AccountId == closing.Id).AccountIsActive.Should().BeFalse();
            rows.Single(row => row.AccountId == _barclays.Id).AccountIsActive.Should().BeTrue();
        }
    }

    [Fact]
    public void GetMonthTransactions_SameDate_OrdersByAccountName()
    {
        var dormant = TestAccounts.Bank("Dormant");
        _repository.Accounts.Add(dormant);
        _repository.BalanceAdjustments.AddRange([
            BalanceAdjustment.Create(new DateOnly(2026, 10, 3), dormant, 0m, 1m, null),
            BalanceAdjustment.Create(new DateOnly(2026, 10, 3), _chase, 0m, 2m, null),
            BalanceAdjustment.Create(new DateOnly(2026, 10, 2), _barclays, 0m, 3m, null),
            BalanceAdjustment.Create(new DateOnly(2026, 10, 3), _barclays, 0m, 4m, null)]);

        var rows = _sut.GetMonthTransactions(2026, 10);

        rows.Select(row => (row.AccountName, row.Transaction.Date.Day)).Should().Equal(
            ("Barclays", 3), ("chase", 3), ("Dormant", 3), ("Barclays", 2));
    }

    [Fact]
    public void GetMonthTransactions_EmptyMonth_ReturnsEmpty()
    {
        SeedEveryRecordKind();

        var rows = _sut.GetMonthTransactions(2026, 7);

        using (new AssertionScope())
        {
            rows.Should().BeEmpty();
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountQueryService.GetMonthTransactions")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(0, 10)]
    public void GetMonthTransactions_InvalidMonth_ThrowsArgumentOutOfRangeAndFailsTheSpan(int year, int month)
    {
        var act = () => _sut.GetMonthTransactions(year, month);

        act.Should().Throw<ArgumentOutOfRangeException>();
        _tracer.ShouldHaveFailedSpan<ArgumentOutOfRangeException>("CashFlow.AccountQueryService.GetMonthTransactions");
    }
}
