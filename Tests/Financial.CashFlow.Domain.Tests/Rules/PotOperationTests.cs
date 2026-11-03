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
    private readonly ReserveBucket _pot = ReserveBucket.Create("Investimento", 100m);

    public PotOperationTests()
    {
        _reserva.AddPot(_pot);
    }

    [Fact]
    public void Read_WithdrawalThroughABank_RebuildsTheFormValuesAndItsRecords()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 38.25m, Date, "Boots", _bank, _health, OperationId);

        var operation = PotOperation.Read(OperationId, [records.Movement], records.BankExpenses, [_bank, _reserva]);

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
            operation.Movements.Should().Equal(records.Movement);
            operation.Expenses.Should().Equal(records.BankExpenses);
        }
    }

    [Fact]
    public void Read_WithdrawalWithoutABank_HasNoBankOrCategory()
    {
        var records = PotWithdrawal.Build(_reserva, _pot, 20m, Date, "Cash", bank: null, category: null, OperationId);

        var operation = PotOperation.Read(OperationId, [records.Movement], [], [_reserva]);

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
        var legacy = ReserveMovement.Create(_pot, -7m, Date, "Cash");

        var operation = PotOperation.Read(
            OperationId, [mine.Movement, other.Movement, legacy], [.. mine.BankExpenses, .. other.BankExpenses], [_reserva]);

        using (new AssertionScope())
        {
            operation!.Amount.Should().Be(20m);
            operation.Movements.Should().Equal(mine.Movement);
            operation.Expenses.Should().Equal(mine.BankExpenses);
        }
    }

    [Fact]
    public void Read_UnknownOperation_ReturnsNull()
    {
        var operation = PotOperation.Read(OperationId, [ReserveMovement.Create(_pot, -7m, Date, "Cash")], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PotThatNoAccountOwns_ReturnsNull()
    {
        var orphan = ReserveBucket.Create("Orphan", 0m);

        var operation = PotOperation.Read(OperationId, [ReserveMovement.Create(orphan, -7m, Date, "Cash", operationId: OperationId)], [], [_reserva]);

        operation.Should().BeNull();
    }

    [Fact]
    public void Read_PositiveMovementsOfAnIncome_ReadAsADepositWithTheirIncome()
    {
        var second = ReserveBucket.Create("HouseTreats", 0m);
        _reserva.AddPot(second);
        var income = Income.Create(Date, IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: _reserva), null, 100m, _bank, potsAccount: _reserva);
        ReserveMovement[] movements =
        [
            ReserveMovement.Create(_pot, 60m, Date, "Split", income, OperationId),
            ReserveMovement.Create(second, 30m, Date, "Split", income, OperationId)
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
}
