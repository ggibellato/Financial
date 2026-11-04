using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class PotsServiceTests
{
    private static readonly DateOnly Date = new(2026, 10, 1);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly PotsService _sut;

    public PotsServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultPots: true, seedDefaultCategories: true);
        Reserva.SetCategory(CategoryNamed("Reserva"));
        _repository.PotTransactions.Add(PotTransaction.Create(PotNamed("Investimento"), 100m, new DateOnly(2026, 1, 1), "Seed"));
        _sut = new PotsService(_repository, TestClock.At(), _tracer, NullLogger<PotsService>.Instance);
    }

    private Account Reserva => _repository.EnsureReserva();

    private Pot PotNamed(string name) => _repository.ReservaPots.First(pot => pot.Name == name);

    private Category CategoryNamed(string name) => _repository.Categories.First(category => category.Name == name);

    private Account Bank(string name) => _repository.Accounts.First(account => account.Name == name);

    private PotWithdrawalRequestDTO Withdrawal(
        decimal amount = 30m, bool withBank = true, bool confirmed = false, DateOnly? date = null, string pot = "Investimento") => new()
    {
        PotId = PotNamed(pot).Id,
        Amount = amount,
        Date = date ?? Date,
        Description = "Dentist",
        BankId = withBank ? Bank("Chase").Id : null,
        CategoryId = withBank ? CategoryNamed("Saude").Id : null,
        Confirmed = confirmed
    };

    private PotDepositRequestDTO Deposit(
        decimal amount = 100m, string? pot = null, DateOnly? date = null, (string Bank, decimal Amount)[]? lines = null) => new()
    {
        Amount = amount,
        Date = date ?? Date,
        Description = "Ramsay bonus",
        PotId = pot is null ? null : PotNamed(pot).Id,
        FundingLines = (lines ?? []).Select(line => new FundingLineDTO { BankId = Bank(line.Bank).Id, Amount = line.Amount }).ToList()
    };

    private Expense AddUnrelatedExpense()
    {
        var expense = Expense.Create(Date, "Groceries", 12m, CategoryNamed("Mercado"), Bank("Chase"), creditCard: null);
        _repository.Expenses.Add(expense);
        return expense;
    }

    [Fact]
    public async Task WithdrawAsync_ThroughABank_SavesTheLinkedRecordsAndReturnsTheOperation()
    {
        var result = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());

        using (new AssertionScope())
        {
            result.Kind.Should().Be("Withdrawal");
            result.PotsAccountId.Should().Be(Reserva.Id);
            result.PotId.Should().Be(PotNamed("Investimento").Id);
            result.Amount.Should().Be(30m);
            result.Date.Should().Be(Date);
            result.Description.Should().Be("Dentist");
            result.BankId.Should().Be(Bank("Chase").Id);
            result.CategoryId.Should().Be(CategoryNamed("Saude").Id);
            result.IncomeId.Should().BeNull();
            _repository.PotTransactions.Should().ContainSingle(m => m.OperationId == result.OperationId && m.Amount == -30m);
            _repository.Expenses.Where(e => e.OperationId == result.OperationId).Select(e => (e.Category.Name, e.Value))
                .Should().BeEquivalentTo([("Reserva", -30m), ("Saude", 30m)]);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task WithdrawAsync_WithoutABank_SavesOnlyTheMovement()
    {
        var result = await _sut.WithdrawAsync(Reserva.Id, Withdrawal(withBank: false));

        using (new AssertionScope())
        {
            result.BankId.Should().BeNull();
            result.CategoryId.Should().BeNull();
            _repository.PotTransactions.Should().ContainSingle(m => m.OperationId == result.OperationId);
            _repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task WithdrawAsync_OverdraftWithoutConfirmation_ThrowsAndSavesNothing()
    {
        var act = async () => await _sut.WithdrawAsync(Reserva.Id, Withdrawal(amount: 150m));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>()
                .WithMessage("This withdrawal exceeds Investimento's balance of 100.00. Set confirmed=true to proceed.");
            _repository.PotTransactions.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<OverdraftConfirmationRequiredException>("CashFlow.PotsService.Withdraw");
        }
    }

    [Fact]
    public async Task WithdrawAsync_OverdraftConfirmed_Saves()
    {
        var result = await _sut.WithdrawAsync(Reserva.Id, Withdrawal(amount: 150m, confirmed: true));

        result.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task WithdrawAsync_FutureDate_Throws()
    {
        var act = async () => await _sut.WithdrawAsync(Reserva.Id, Withdrawal(date: TestClock.Today.AddDays(1)));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Withdraw");
        }
    }

    [Fact]
    public async Task WithdrawAsync_PotOfAnotherAccount_Throws()
    {
        var other = TestAccounts.Pots("Holidays");
        var foreignPot = Pot.Create("Trip", 100m);
        other.AddPot(foreignPot);
        _repository.Accounts.Add(other);
        var request = Withdrawal();

        var act = async () => await _sut.WithdrawAsync(Reserva.Id, new PotWithdrawalRequestDTO
        {
            PotId = foreignPot.Id,
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("That pot isn't in Reserva.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Withdraw");
        }
    }

    [Fact]
    public async Task WithdrawAsync_AccountWithoutPots_ThrowsNotFound()
    {
        var act = async () => await _sut.WithdrawAsync(Bank("Chase").Id, Withdrawal());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.Withdraw");
        }
    }

    [Fact]
    public async Task GetOperation_SavedWithdrawal_ReturnsItsFormValues()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());

        var result = _sut.GetOperation(created.OperationId);

        result.Should().BeEquivalentTo(created);
    }

    [Fact]
    public void GetOperation_UnknownId_ThrowsNotFound()
    {
        var act = () => _sut.GetOperation(Guid.NewGuid());

        using (new AssertionScope())
        {
            act.Should().Throw<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.GetOperation");
        }
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_ChangedValues_RebuildsEveryRecordUnderTheSameIdInOneSave()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());
        var unrelated = AddUnrelatedExpense();

        var result = await _sut.UpdateWithdrawalAsync(Reserva.Id, created.OperationId, new PotWithdrawalRequestDTO
        {
            PotId = PotNamed("Investimento").Id,
            Amount = 45m,
            Date = new DateOnly(2026, 9, 30),
            Description = "Optician",
            BankId = Bank("Barclays").Id,
            CategoryId = CategoryNamed("Familia").Id
        });

        using (new AssertionScope())
        {
            result.OperationId.Should().Be(created.OperationId);
            result.Amount.Should().Be(45m);
            result.BankId.Should().Be(Bank("Barclays").Id);
            _repository.PotTransactions.Where(m => m.OperationId == created.OperationId).Select(m => (m.Amount, m.Description, m.Date))
                .Should().Equal((-45m, "Optician", new DateOnly(2026, 9, 30)));
            _repository.Expenses.Where(e => e.OperationId == created.OperationId).Select(e => (e.Category.Name, e.Value, e.PaymentSourceAccount!.Name))
                .Should().BeEquivalentTo([("Reserva", -45m, "Barclays"), ("Familia", 45m, "Barclays")]);
            _repository.Expenses.Should().Contain(unrelated).And.HaveCount(3);
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_RemovingTheBank_LeavesOnlyTheMovement()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());

        await _sut.UpdateWithdrawalAsync(Reserva.Id, created.OperationId, Withdrawal(withBank: false));

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().ContainSingle(m => m.OperationId == created.OperationId);
            _repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_OverdraftCheck_DoesNotCountTheOperationItself()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal(amount: 80m));

        var result = await _sut.UpdateWithdrawalAsync(Reserva.Id, created.OperationId, Withdrawal(amount: 90m));

        result.Amount.Should().Be(90m);
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_OverdraftWithoutConfirmation_ThrowsAndKeepsTheRecords()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());

        var act = async () => await _sut.UpdateWithdrawalAsync(Reserva.Id, created.OperationId, Withdrawal(amount: 101m));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>();
            _repository.PotTransactions.Should().ContainSingle(m => m.OperationId == created.OperationId && m.Amount == -30m);
            _repository.Expenses.Should().HaveCount(2);
            _tracer.ShouldHaveFailedSpan<OverdraftConfirmationRequiredException>("CashFlow.PotsService.UpdateWithdrawal");
        }
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_UnderAnotherAccount_ThrowsNotFound()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());
        var other = TestAccounts.Pots("Holidays");
        _repository.Accounts.Add(other);

        var act = async () => await _sut.UpdateWithdrawalAsync(other.Id, created.OperationId, Withdrawal());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.UpdateWithdrawal");
        }
    }

    [Fact]
    public async Task UpdateWithdrawalAsync_ADeposit_ThrowsNotFound()
    {
        var operationId = Guid.NewGuid();
        _repository.PotTransactions.Add(PotTransaction.Create(PotNamed("HouseTreats"), 50m, Date, "Split", operationId: operationId));

        var act = async () => await _sut.UpdateWithdrawalAsync(Reserva.Id, operationId, Withdrawal());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.UpdateWithdrawal");
        }
    }

    [Fact]
    public async Task DeleteOperationAsync_Withdrawal_RemovesEveryRecordInOneSave()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());
        var unrelated = AddUnrelatedExpense();

        await _sut.DeleteOperationAsync(created.OperationId);

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().ContainSingle().Which.Description.Should().Be("Seed");
            _repository.Expenses.Should().Equal(unrelated);
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task DeleteOperationAsync_AnIncomesDeposit_ThrowsConflictAndKeepsIt()
    {
        var operationId = Guid.NewGuid();
        var income = Income.Create(Date, IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: Reserva), null, 100m, Bank("Chase"), potsAccount: Reserva);
        _repository.PotTransactions.Add(PotTransaction.Create(PotNamed("HouseTreats"), 90m, Date, "Split", income, operationId));

        var act = async () => await _sut.DeleteOperationAsync(operationId);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>().WithMessage("This deposit belongs to an income; edit the income.");
            _repository.PotTransactions.Should().HaveCount(2);
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.PotsService.DeleteOperation");
        }
    }

    private PotsUpdateDTO PotsUpdate(params PotUpdateDTO[] added) => new()
    {
        Pots =
        [
            .. Reserva.Pots!.Select(pot => new PotUpdateDTO
            {
                Id = pot.Id,
                Name = pot.Name,
                SplitPercentage = pot.SplitPercentage,
                IsActive = pot.IsActive
            }),
            .. added
        ]
    };

    [Fact]
    public async Task ReplacePotsAsync_ValidList_SavesOnceWithANewPot()
    {
        var existingCount = Reserva.Pots!.Count;

        await _sut.ReplacePotsAsync(Reserva.Id, PotsUpdate(new PotUpdateDTO { Name = "Holidays", SplitPercentage = 0m, IsActive = false }));

        using (new AssertionScope())
        {
            Reserva.Pots!.Should().HaveCount(existingCount + 1);
            PotNamed("Holidays").IsActive.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task ReplacePotsAsync_TotalNot100_ThrowsAndSavesNothing()
    {
        var act = async () => await _sut.ReplacePotsAsync(Reserva.Id, PotsUpdate(new PotUpdateDTO { Name = "Holidays", SplitPercentage = 5m, IsActive = true }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Active pots must total 100% (now 105%).");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.ReplacePots");
        }
    }

    [Fact]
    public async Task ReplacePotsAsync_DuplicateName_ThrowsConflictAndSavesNothing()
    {
        var act = async () => await _sut.ReplacePotsAsync(Reserva.Id, PotsUpdate(new PotUpdateDTO { Name = "INVESTIMENTO", SplitPercentage = 0m, IsActive = false }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>().WithMessage("A pot named \"Investimento\" already exists.");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<DuplicateNameException>("CashFlow.PotsService.ReplacePots");
        }
    }

    [Fact]
    public async Task ReplacePotsAsync_AccountWithoutPots_ThrowsNotFound()
    {
        var act = async () => await _sut.ReplacePotsAsync(Bank("Chase").Id, PotsUpdate());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.ReplacePots");
        }
    }

    [Fact]
    public async Task DeleteOperationAsync_UnknownId_ThrowsNotFound()
    {
        var act = async () => await _sut.DeleteOperationAsync(Guid.NewGuid());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.DeleteOperation");
        }
    }

    [Fact]
    public async Task DepositAsync_IntoActivePotsWithFundingLines_SavesTheLinkedRecordsAndReturnsTheOperation()
    {
        var result = await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Chase", 85m), ("Barclays", 15m)]));

        using (new AssertionScope())
        {
            result.Kind.Should().Be("Deposit");
            result.Into.Should().Be("ActivePots");
            result.PotId.Should().BeNull();
            result.Amount.Should().Be(100m);
            result.Description.Should().Be("Ramsay bonus");
            result.FundingLines.Select(line => (line.BankId, line.Amount))
                .Should().Equal((Bank("Chase").Id, 85m), (Bank("Barclays").Id, 15m));
            _repository.PotTransactions.Where(m => m.OperationId == result.OperationId).Select(m => (m.Pot.Name, m.Amount))
                .Should().Equal(("Investimento", 33.33m), ("HouseTreats", 33.33m), ("Ariana", 16.67m), ("Gleison", 16.67m));
            _repository.Expenses.Where(e => e.OperationId == result.OperationId).Select(e => (e.Category.Name, e.Value, e.PaymentSourceAccount!.Name))
                .Should().Equal(("Reserva", 85m, "Chase"), ("Reserva", 15m, "Barclays"));
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DepositAsync_IntoOnePotWithoutLines_CreditsOnlyThatPot()
    {
        var result = await _sut.DepositAsync(Reserva.Id, Deposit(amount: 40m, pot: "Ariana"));

        using (new AssertionScope())
        {
            result.Into.Should().Be("OnePot");
            result.PotId.Should().Be(PotNamed("Ariana").Id);
            result.FundingLines.Should().BeEmpty();
            _repository.PotTransactions.Should().ContainSingle(m => m.OperationId == result.OperationId && m.Pot == PotNamed("Ariana") && m.Amount == 40m);
            _repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task DepositAsync_LinesNotAddingUp_ThrowsAndSavesNothing()
    {
        var act = async () => await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Chase", 80m)]));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Funding lines must add up to £100.00.");
            _repository.PotTransactions.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task DepositAsync_UnknownFundingBank_Throws()
    {
        var request = Deposit();
        var act = async () => await _sut.DepositAsync(Reserva.Id, new PotDepositRequestDTO
        {
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description,
            FundingLines = [new FundingLineDTO { BankId = Guid.NewGuid(), Amount = 100m }]
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Payment source '*' is not recognized.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task DepositAsync_FromAnInactiveBank_Throws()
    {
        var closed = TestAccounts.Bank("Closed", isActive: false);
        _repository.Accounts.Add(closed);

        var act = async () => await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Closed", 100m)]));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task DepositAsync_PotOfAnotherAccount_Throws()
    {
        var other = TestAccounts.Pots("Holidays");
        var foreignPot = Pot.Create("Trip", 100m);
        other.AddPot(foreignPot);
        _repository.Accounts.Add(other);
        var request = Deposit();

        var act = async () => await _sut.DepositAsync(Reserva.Id, new PotDepositRequestDTO
        {
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description,
            PotId = foreignPot.Id
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("That pot isn't in Reserva.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task DepositAsync_FutureDate_Throws()
    {
        var act = async () => await _sut.DepositAsync(Reserva.Id, Deposit(date: TestClock.Today.AddDays(1)));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task DepositAsync_AccountWithoutPots_ThrowsNotFound()
    {
        var act = async () => await _sut.DepositAsync(Bank("Chase").Id, Deposit());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.Deposit");
        }
    }

    [Fact]
    public async Task GetOperation_SavedDeposit_ReturnsItsFormValues()
    {
        var created = await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Chase", 100m)]));

        var result = _sut.GetOperation(created.OperationId);

        result.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task UpdateDepositAsync_ChangedValues_RebuildsEveryRecordUnderTheSameIdInOneSave()
    {
        var created = await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Chase", 100m)]));
        var unrelated = AddUnrelatedExpense();

        var result = await _sut.UpdateDepositAsync(Reserva.Id, created.OperationId, Deposit(amount: 60m, pot: "Gleison", lines: [("Barclays", 60m)]));

        using (new AssertionScope())
        {
            result.OperationId.Should().Be(created.OperationId);
            result.Into.Should().Be("OnePot");
            result.Amount.Should().Be(60m);
            _repository.PotTransactions.Where(m => m.OperationId == created.OperationId).Select(m => (m.Pot.Name, m.Amount))
                .Should().Equal(("Gleison", 60m));
            _repository.Expenses.Where(e => e.OperationId == created.OperationId).Select(e => (e.Value, e.PaymentSourceAccount!.Name))
                .Should().Equal((60m, "Barclays"));
            _repository.Expenses.Should().Contain(unrelated).And.HaveCount(2);
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateDepositAsync_AWithdrawal_ThrowsNotFound()
    {
        var created = await _sut.WithdrawAsync(Reserva.Id, Withdrawal());

        var act = async () => await _sut.UpdateDepositAsync(Reserva.Id, created.OperationId, Deposit());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _repository.Expenses.Should().HaveCount(2);
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.UpdateDeposit");
        }
    }

    [Fact]
    public async Task UpdateDepositAsync_UnderAnotherAccount_ThrowsNotFound()
    {
        var created = await _sut.DepositAsync(Reserva.Id, Deposit());
        var other = TestAccounts.Pots("Holidays");
        _repository.Accounts.Add(other);

        var act = async () => await _sut.UpdateDepositAsync(other.Id, created.OperationId, Deposit());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.PotsService.UpdateDeposit");
        }
    }

    [Fact]
    public async Task UpdateDepositAsync_AnIncomesDeposit_ThrowsConflictAndKeepsIt()
    {
        var operationId = Guid.NewGuid();
        var income = Income.Create(Date, IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: Reserva), null, 100m, Bank("Chase"), potsAccount: Reserva);
        _repository.PotTransactions.Add(PotTransaction.Create(PotNamed("HouseTreats"), 90m, Date, "Split", income, operationId));

        var act = async () => await _sut.UpdateDepositAsync(Reserva.Id, operationId, Deposit());

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>().WithMessage("This deposit belongs to an income; edit the income.");
            _repository.PotTransactions.Should().HaveCount(2);
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.PotsService.UpdateDeposit");
        }
    }

    [Fact]
    public async Task DeleteOperationAsync_Deposit_RemovesEveryRecordInOneSave()
    {
        var created = await _sut.DepositAsync(Reserva.Id, Deposit(lines: [("Chase", 100m)]));

        await _sut.DeleteOperationAsync(created.OperationId);

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().ContainSingle().Which.Description.Should().Be("Seed");
            _repository.Expenses.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }
}
