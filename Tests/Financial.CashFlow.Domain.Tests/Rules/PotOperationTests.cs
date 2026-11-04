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
    private static readonly Guid OperationId = Guid.NewGuid();

    private readonly Category _health = Category.Create("Saude");
    private readonly Account _bank = TestAccounts.Bank("Chase");
    private readonly Account _reserva = TestAccounts.Pots("Reserva", category: Category.Create("Reserva"));
    private readonly Pot _pot = Pot.Create("Investimento", 100m);

    public PotOperationTests()
    {
        _reserva.AddPot(_pot);
    }

    [Fact]
    public void Read_WithdrawalThroughABank_RebuildsTheFormValuesAndItsRecords()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, records.Expenses, [_bank, _reserva]);

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
            operation.Movements.Should().Equal(records.Movements);
            operation.Expenses.Should().Equal(records.Expenses);
        }
    }

    [Fact]
    public void Read_WithdrawalWithoutABank_HasNoBankOrCategory()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, [], [_reserva]);

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
        var mine = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", _bank, _health, OperationId);
        var other = PotWithdrawal.Build(_reserva, _pot, 5m, Date, "Cash", _bank, _health, Guid.NewGuid());
        var legacy = PotMovement.Create(_pot, -7m, Date, "Cash");

        var operation = PotOperation.Read(
            OperationId, [.. mine.Movements, .. other.Movements, legacy], [.. mine.Expenses, .. other.Expenses], [_reserva]);

        using (new AssertionScope())
        {
            operation!.Amount.Should().Be(20m);
            operation.Movements.Should().Equal(mine.Movements);
            operation.Expenses.Should().Equal(mine.Expenses);
        }
    }

    [Fact]
    public void Read_UnknownOperation_ReturnsNull()
    {
        var operation = PotOperation.Read(OperationId, [PotMovement.Create(_pot, -7m, Date, "Cash")], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PotThatNoAccountOwns_ReturnsNull()
    {
        var orphan = Pot.Create("Orphan", 0m);

        var operation = PotOperation.Read(OperationId, [PotMovement.Create(orphan, -7m, Date, "Cash", operationId: OperationId)], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PositiveMovementsOfAnIncome_ReadAsADepositWithTheirIncome()
    {
        var second = Pot.Create("HouseTreats", 0m);
        _reserva.AddPot(second);
        var income = Income.Create(Date, IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: _reserva), null, 100m, _bank, potsAccount: _reserva);
        PotMovement[] movements =
        [
            PotMovement.Create(_pot, 60m, Date, "Split", income, OperationId),
            PotMovement.Create(second, 30m, Date, "Split", income, OperationId)
        ];

        var operation = PotOperation.Read(OperationId, movements, [], [_reserva]);

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
        var records = PotDeposit.Build(_reserva, 90m, Date, "Bonus", pot: null, [new(_bank, 85m), new(barclays, 5m)], OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, records.Expenses, [_bank, barclays, _reserva]);

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
        var records = PotDeposit.Build(_reserva, 40m, Date, "Gift", _pot, [], OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, records.Expenses, [_reserva]);

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
        var records = PotDeposit.Build(_reserva, 40m, Date, "Gift", pot: null, [], OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, records.Expenses, [_reserva]);

        (operation!.Into, operation.Pot).Should().Be((PotDepositTarget.OnePot, _pot));
    }

    [Fact]
    public void Read_Withdrawal_HasNoTargetOrFundingLines()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", _bank, _health, OperationId);

        var operation = PotOperation.Read(OperationId, records.Movements, records.Expenses, [_bank, _reserva]);

        using (new AssertionScope())
        {
            operation!.Into.Should().BeNull();
            operation.FundingLines.Should().BeEmpty();
        }
    }
}
