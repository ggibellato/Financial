using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class PotOperationTests
{
    private static readonly DateOnly Date = new(2026, 10, 12);
    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly Guid OperationId = Guid.NewGuid();

    private readonly Category _health = Category.Create("Saude");
    private readonly Account _bank = TestAccounts.Bank("Chase");
    private readonly Account _reserva = TestAccounts.Pots("Reserva", category: Category.Create("Reserva"));
    private readonly Pot _pot = Pot.Create("Investimento", 100m);
    private readonly Account _card = TestAccounts.CreditCard("Barclaycard");

    public PotOperationTests()
    {
        _reserva.AddPot(_pot);
    }

    [Fact]
    public void Read_WithdrawalThroughABank_RebuildsTheFormValuesAndItsRecords()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_bank, _reserva]);

        using (new AssertionScope())
        {
            operation!.Kind.Should().Be(PotOperationKind.Withdrawal);
            operation.PotsAccount.Should().BeSameAs(_reserva);
            operation.Date.Should().Be(Date);
            operation.Description.Should().Be("Boots");
            operation.Amount.Should().Be(38.25m);
            operation.Pot.Should().BeSameAs(_pot);
            operation.Bank.Should().BeSameAs(_bank);
            operation.Category.Should().BeSameAs(_health);
            operation.IncomeId.Should().BeNull();
            operation.Transactions.Should().Equal(records.Transactions);
            operation.Expenses.Should().Equal(records.Expenses);
        }
    }

    [Fact]
    public void Read_WithdrawalWithoutABank_HasNoBankOrCategory()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, [], [_reserva]);

        using (new AssertionScope())
        {
            operation!.Kind.Should().Be(PotOperationKind.Withdrawal);
            operation.Amount.Should().Be(20m);
            operation.Bank.Should().BeNull();
            operation.Category.Should().BeNull();
            operation.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public void Read_RecordsOfOtherOperationsOrNone_AreLeftOut()
    {
        var mine = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", _bank, _health, OperationId, Today);
        var other = PotWithdrawal.Build(_reserva, _pot, 5m, Date, "Cash", _bank, _health, Guid.NewGuid(), Today);
        var legacy = PotTransaction.Create(_pot, -7m, Date, "Cash");

        var operation = PotOperation.Read(
            OperationId, [.. mine.Transactions, .. other.Transactions, legacy], [.. mine.Expenses, .. other.Expenses], [_reserva]);

        using (new AssertionScope())
        {
            operation!.Amount.Should().Be(20m);
            operation.Transactions.Should().Equal(mine.Transactions);
            operation.Expenses.Should().Equal(mine.Expenses);
        }
    }

    [Fact]
    public void Read_UnknownOperation_ReturnsNull()
    {
        var operation = PotOperation.Read(OperationId, [PotTransaction.Create(_pot, -7m, Date, "Cash")], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PotThatNoAccountOwns_ReturnsNull()
    {
        var orphan = Pot.Create("Orphan", 0m);

        var operation = PotOperation.Read(OperationId, [PotTransaction.Create(orphan, -7m, Date, "Cash", operationId: OperationId)], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PositiveTransactionsOfAnIncome_ReadAsADepositWithTheirIncome()
    {
        var second = Pot.Create("HouseTreats", 0m);
        _reserva.AddPot(second);
        var income = Income.Create(Date, IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: _reserva), null, 100m, _bank, potsAccount: _reserva);
        PotTransaction[] transactions =
        [
            PotTransaction.Create(_pot, 60m, Date, "Split", income, OperationId),
            PotTransaction.Create(second, 30m, Date, "Split", income, OperationId)
        ];

        var operation = PotOperation.Read(OperationId, transactions, [], [_reserva]);

        using (new AssertionScope())
        {
            operation!.Kind.Should().Be(PotOperationKind.Deposit);
            operation.Amount.Should().Be(90m);
            operation.Pot.Should().BeNull();
            operation.IncomeId.Should().Be(income.Id);
        }
    }

    [Fact]
    public void Read_DepositIntoActivePotsWithFundingLines_RebuildsTheFormValues()
    {
        var second = Pot.Create("HouseTreats", 0m);
        _reserva.AddPot(second);
        var barclays = TestAccounts.Bank("Barclays");
        var records = PotDeposit.Build(_reserva, 90m, Date, "Bonus", pot: null, [new(_bank, 85m), new(barclays, 5m)], OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_bank, barclays, _reserva]);

        using (new AssertionScope())
        {
            operation!.Kind.Should().Be(PotOperationKind.Deposit);
            operation.Into.Should().Be(PotDepositTarget.ActivePots);
            operation.Pot.Should().BeNull();
            operation.Amount.Should().Be(90m);
            operation.Description.Should().Be("Bonus");
            operation.FundingLines.Should().Equal(new PotFundingLine(_bank, 85m), new PotFundingLine(barclays, 5m));
            operation.Bank.Should().BeNull();
            operation.Category.Should().BeNull();
        }
    }

    [Fact]
    public void Read_DepositIntoOnePotWithoutLines_ReadsBackAsThatPot()
    {
        var records = PotDeposit.Build(_reserva, 40m, Date, "Gift", _pot, [], OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_reserva]);

        using (new AssertionScope())
        {
            operation!.Into.Should().Be(PotDepositTarget.OnePot);
            operation.Pot.Should().BeSameAs(_pot);
            operation.Amount.Should().Be(40m);
            operation.FundingLines.Should().BeEmpty();
        }
    }

    [Fact]
    public void Read_SplitIntoTheOnlyActivePot_ReadsBackAsThatPot()
    {
        var records = PotDeposit.Build(_reserva, 40m, Date, "Gift", pot: null, [], OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_reserva]);

        (operation!.Into, operation.Pot).Should().Be((PotDepositTarget.OnePot, _pot));
    }

    [Fact]
    public void Read_Withdrawal_HasNoTargetOrFundingLines()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", _bank, _health, OperationId, Today);

        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_bank, _reserva]);

        using (new AssertionScope())
        {
            operation!.Into.Should().BeNull();
            operation.FundingLines.Should().BeEmpty();
        }
    }

    [Fact]
    public void Read_CardWithdrawal_ExposesTheCardStatementAndAPendingInvestmentWithdrawal()
    {
        var operation = ReadCardWithdrawal();

        using (new AssertionScope())
        {
            operation.CreditCard.Should().BeSameAs(_card);
            operation.StatementMonth.Should().Be(new DateOnly(2026, 11, 1));
            operation.InvestmentWithdrawal.Should().Be(InvestmentWithdrawalStatus.Pending);
            operation.Bank.Should().BeNull();
            operation.Category.Should().BeSameAs(_health);
        }
    }

    [Fact]
    public void Read_CardWithdrawalWithANegativeExpenseInThePotsCategory_IsDone()
    {
        var records = BuildByCard();
        var investmentWithdrawal = InvestmentWithdrawalExpense();

        var operation = PotOperation.Read(OperationId, records.Transactions, [.. records.Expenses, investmentWithdrawal], [_bank, _card, _reserva]);

        operation!.InvestmentWithdrawal.Should().Be(InvestmentWithdrawalStatus.Done);
    }

    [Fact]
    public void Read_BankOrPotOnlyWithdrawal_HasNoCardOrInvestmentWithdrawalStatus()
    {
        var throughBank = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Boots", _bank, _health, OperationId, Today);
        var potOnly = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, Guid.NewGuid(), Today);

        var operations = new[]
        {
            PotOperation.Read(throughBank.OperationId, throughBank.Transactions, throughBank.Expenses, [_bank, _reserva])!,
            PotOperation.Read(potOnly.OperationId, potOnly.Transactions, [], [_reserva])!
        };

        operations.Should().OnlyContain(operation =>
            operation.CreditCard == null && operation.StatementMonth == null && operation.InvestmentWithdrawal == null);
    }

    [Fact]
    public void Relabel_ChangesTheDescriptionOfEveryRecordAndTheChosenCategoryKeepingTheirIds()
    {
        var records = BuildByCard();
        var investmentWithdrawal = InvestmentWithdrawalExpense();
        var operation = PotOperation.Read(OperationId, records.Transactions, [.. records.Expenses, investmentWithdrawal], [_bank, _card, _reserva])!;
        var ids = operation.Expenses.Select(expense => expense.Id).Append(operation.Transactions[0].Id).ToList();
        var travel = Category.Create("Viagem");

        operation.Relabel("Flights to Lisbon", travel);

        using (new AssertionScope())
        {
            operation.Expenses.Select(expense => expense.Id).Append(operation.Transactions[0].Id).Should().Equal(ids);
            operation.Transactions[0].Description.Should().Be("Flights to Lisbon");
            operation.Expenses.Should().OnlyContain(expense => expense.Description == "Flights to Lisbon");
            operation.Category.Should().BeSameAs(travel);
            investmentWithdrawal.Category.Should().BeSameAs(_reserva.Category);
            operation.Amount.Should().Be(120m);
            operation.StatementMonth.Should().Be(new DateOnly(2026, 11, 1));
        }
    }

    private PotOperation ReadCardWithdrawal()
    {
        var records = BuildByCard();
        return PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_card, _reserva])!;
    }

    private PotOperation BuildByCard() =>
        PotWithdrawal.Build(
            _reserva, _pot, 120m, Date, "Flights", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 11, 1)));

    private Expense InvestmentWithdrawalExpense() =>
        ReadCardWithdrawal().WithdrawFromInvestment(_bank, new DateOnly(2026, 11, 3));

    [Fact]
    public void WithdrawFromInvestment_WritesMinusXOnTheBankInThePotsCategoryReportedInTheStatementMonth()
    {
        var operation = ReadCardWithdrawal();

        var withdrawal = operation.WithdrawFromInvestment(_bank, new DateOnly(2026, 12, 3));

        using (new AssertionScope())
        {
            withdrawal.Value.Should().Be(-120m);
            withdrawal.Date.Should().Be(new DateOnly(2026, 12, 3));
            withdrawal.PaymentSourceAccount.Should().BeSameAs(_bank);
            withdrawal.CreditCard.Should().BeNull();
            withdrawal.Category.Should().BeSameAs(_reserva.Category);
            withdrawal.Description.Should().Be("Flights");
            withdrawal.OperationId.Should().Be(OperationId);
            withdrawal.ReportingMonth.Should().Be(new DateOnly(2026, 11, 1));
            withdrawal.CountsAsTithe.Should().BeTrue();
        }
    }

    [Fact]
    public void WithdrawFromInvestment_PotsAccountWithoutACategory_Throws()
    {
        var uncategorised = TestAccounts.Pots("Savings");
        var pot = Pot.Create("Car", 100m);
        uncategorised.AddPot(pot);
        var records = PotWithdrawal.Build(
            uncategorised, pot, 50m, Date, "Tyres", bank: null, _health, OperationId, Today, new PotWithdrawalCard(_card, new DateOnly(2026, 10, 1)));
        var operation = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_card, uncategorised])!;

        var act = () => operation.WithdrawFromInvestment(_bank, new DateOnly(2026, 11, 3));

        act.Should().Throw<ArgumentException>().WithMessage("Set a category on Savings first.");
    }

    [Fact]
    public void WithdrawFromInvestment_BankWithdrawal_Throws()
    {
        var act = () => ReadBankWithdrawal().WithdrawFromInvestment(_bank, Date);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void InvestmentWithdrawal_IsRecognisedByItsBankEvenAfterThePotsCategoryChanges()
    {
        var records = BuildByCard();
        var withdrawal = PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_card, _reserva])!
            .WithdrawFromInvestment(_bank, new DateOnly(2026, 12, 3));
        _reserva.SetCategory(Category.Create("Savings"));

        var operation = PotOperation.Read(OperationId, records.Transactions, [.. records.Expenses, withdrawal], [_bank, _card, _reserva])!;

        using (new AssertionScope())
        {
            operation.InvestmentWithdrawal.Should().Be(InvestmentWithdrawalStatus.Done);
            operation.InvestmentWithdrawalExpense.Should().BeSameAs(withdrawal);
        }
    }

    [Fact]
    public void InvestmentWithdrawalExpense_BankWithdrawal_IsNull() =>
        ReadBankWithdrawal().InvestmentWithdrawalExpense.Should().BeNull();

    [Fact]
    public void ReadOnStatement_ReadsTheCardWithdrawalsWhoseChargeIsOnThatStatementOnly()
    {
        var november = BuildByCard();
        var december = PotWithdrawal.Build(
            _reserva, _pot, 30m, Date, "Hotel", bank: null, _health, Guid.NewGuid(), Today, new PotWithdrawalCard(_card, new DateOnly(2026, 12, 1)));
        var unlinked = Expense.Create(Date, "Groceries", 12m, _health, paymentSourceAccount: null, _card, new DateOnly(2026, 11, 1));

        var operations = PotOperation.ReadOnStatement(
            _card, 2026, 11, [.. november.Transactions, .. december.Transactions], [.. november.Expenses, .. december.Expenses, unlinked], [_card, _reserva]);

        operations.Select(operation => operation.OperationId).Should().Equal(OperationId);
    }

    private PotOperation ReadBankWithdrawal()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Boots", _bank, _health, OperationId, Today);
        return PotOperation.Read(OperationId, records.Transactions, records.Expenses, [_bank, _reserva])!;
    }
}
