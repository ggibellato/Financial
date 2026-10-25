using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

[Trait("Category", "Integration")]
public class FailedSaveRollbackTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);

    private readonly SwitchableFailingJsonStorage _storage;
    private readonly CashFlowJsonRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;

    public FailedSaveRollbackTests()
    {
        _storage = new SwitchableFailingJsonStorage();
        _repository = new CashFlowJsonRepository(CreateSeededData(), _storage, new CashFlowSerializerAdapter());
        _tracer = new RecordingTelemetryTracer();
    }

    private static CashFlowData CreateSeededData()
    {
        var data = CashFlowData.Create();
        foreach (var bank in StubCashFlowRepository.DefaultBanks())
        {
            data.AddAccount(bank);
        }

        foreach (var source in StubCashFlowRepository.DefaultIncomeSources())
        {
            data.AddIncomeSource(source);
        }

        foreach (var bucket in StubCashFlowRepository.DefaultReserveBuckets())
        {
            data.AddReserveBucket(bucket);
        }

        foreach (var card in StubCashFlowRepository.DefaultCreditCards())
        {
            data.AddAccount(card);
        }

        foreach (var category in StubCashFlowRepository.DefaultCategories())
        {
            data.AddCategory(category);
        }

        return data;
    }

    private IncomeService CreateIncomeService() => new(_repository, _tracer, NullLogger<IncomeService>.Instance);

    private ReserveService CreateReserveService() => new(_repository, _tracer, NullLogger<ReserveService>.Instance);

    private CardStatementService CreateCardStatementService() =>
        new(_repository, TestClock.At(), NullLogger<CardStatementService>.Instance, _tracer);

    private Guid BankId(string name) => _repository.GetAccounts().Single(b => b.Name == name).Id;

    private IncomeCreateDTO ArianaSplitIncome() => new()
    {
        Date = new DateOnly(2026, 7, 25),
        IncomeSourceId = _repository.GetIncomeSources().Single(s => s.Name == "Ariana").Id,
        NetValue = 2450.00m,
        BankId = BankId("Barclays"),
        SplitToReserve = true
    };

    private IncomeUpdateDTO ArianaSplitIncomeUpdate(decimal netValue) => new()
    {
        Date = new DateOnly(2026, 7, 25),
        IncomeSourceId = _repository.GetIncomeSources().Single(s => s.Name == "Ariana").Id,
        NetValue = netValue,
        BankId = BankId("Barclays"),
        SplitToReserve = true
    };

    [Fact]
    public async Task AddIncomeAsync_WithSplit_WhenTheWriteFails_LeavesNeitherTheIncomeNorAnyMovement()
    {
        _storage.FailWrites = true;

        var act = async () => await CreateIncomeService().AddIncomeAsync(ArianaSplitIncome());

        await act.Should().ThrowAsync<IOException>();
        using (new AssertionScope())
        {
            _repository.GetIncomes().Should().BeEmpty();
            _repository.GetReserveMovements().Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithSplit_WhenTheWriteFails_KeepsTheIncomeAndItsOriginalMovements()
    {
        var service = CreateIncomeService();
        var added = await service.AddIncomeAsync(ArianaSplitIncome());
        var originalMovementIds = _repository.GetReserveMovements().Select(m => m.Id).ToList();
        _storage.FailWrites = true;

        var act = async () => await service.UpdateIncomeAsync(added.Id, ArianaSplitIncomeUpdate(1000.00m));

        await act.Should().ThrowAsync<IOException>();
        using (new AssertionScope())
        {
            _repository.GetIncomes().Should().ContainSingle().Which.NetValue.Should().Be(2450.00m);
            _repository.GetReserveMovements().Select(m => m.Id).Should().BeEquivalentTo(originalMovementIds);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WhenTheWriteFails_LeavesNoMovementInAnyBucket()
    {
        _storage.FailWrites = true;

        var act = async () => await CreateReserveService().PostIncomeSplitAsync(new IncomeSplitRequestDTO
        {
            Date = July1,
            Amount = 1963m,
            Description = "Ramsay"
        });

        await act.Should().ThrowAsync<IOException>();
        _repository.GetReserveMovements().Should().BeEmpty();
    }

    [Fact]
    public async Task PostWithdrawalAsync_ThroughABank_WhenTheWriteFails_LeavesNoMovementAndNoExpense()
    {
        var investimento = _repository.GetReserveBuckets().Single(b => b.Name == "Investimento");
        await _repository.ApplyAndSaveAsync(() =>
        {
            _repository.AddReserveMovement(ReserveMovement.Create(investimento, 100m, July1, "Seed"));
            return true;
        });
        _storage.FailWrites = true;

        var act = async () => await CreateReserveService().PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = investimento.Id,
            Amount = 30m,
            Date = July1,
            Description = "Dentist",
            PaymentSourceBankId = BankId("Chase"),
            ExpenseCategoryId = _repository.GetCategories().Single(c => c.Name == "Saude").Id
        });

        await act.Should().ThrowAsync<IOException>();
        using (new AssertionScope())
        {
            _repository.GetReserveMovements().Should().ContainSingle().Which.Description.Should().Be("Seed");
            _repository.GetExpenses().Should().BeEmpty();
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenTheWriteFails_LeavesTheStatementAndItsChargesUnpaid()
    {
        var service = CreateCardStatementService();
        var statementId = await AddChargeAndGetStatementIdAsync(service);
        _storage.FailWrites = true;

        var act = async () => await service.MarkStatementPaidAsync(statementId, new MarkCardStatementPaidDTO { PaymentSourceBankId = BankId("Barclays") });

        await act.Should().ThrowAsync<IOException>();
        using (new AssertionScope())
        {
            _repository.GetCardStatements().Single(s => s.Id == statementId).IsPaid.Should().BeFalse();
            var charge = _repository.GetExpenses().Should().ContainSingle().Subject;
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            charge.PaymentSourceAccount.Should().BeNull();
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WhenTheWriteFails_LeavesTheStatementAndItsChargesSettled()
    {
        var service = CreateCardStatementService();
        var statementId = await AddChargeAndGetStatementIdAsync(service);
        await service.MarkStatementPaidAsync(statementId, new MarkCardStatementPaidDTO { PaymentSourceBankId = BankId("Trading212") });
        _storage.FailWrites = true;

        var act = async () => await service.UnmarkStatementPaidAsync(statementId);

        await act.Should().ThrowAsync<IOException>();
        using (new AssertionScope())
        {
            _repository.GetCardStatements().Single(s => s.Id == statementId).IsPaid.Should().BeTrue();
            var charge = _repository.GetExpenses().Should().ContainSingle().Subject;
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
            charge.PaymentSourceAccount!.Name.Should().Be("Trading212");
        }
    }

    private async Task<Guid> AddChargeAndGetStatementIdAsync(CardStatementService service)
    {
        var card = _repository.GetAccounts().Single(c => c.Name == "BarclaysPlatinumVisa8003");
        var mercado = _repository.GetCategories().Single(c => c.Name == "Mercado");
        await _repository.ApplyAndSaveAsync(() =>
        {
            _repository.AddExpense(Expense.Create(new DateOnly(2026, 7, 10), "Charge", 30m, mercado, null, card));
            return true;
        });

        await service.GetStatementsForMonthAsync(2026, 7);
        return _repository.GetCardStatements().Single(s => s.CreditCard.Id == card.Id).Id;
    }

    private sealed class SwitchableFailingJsonStorage : IJsonStorage
    {
        public bool FailWrites { get; set; }

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the data directly.");

        public Task WriteAsync(string json) =>
            FailWrites
                ? throw new IOException("The process cannot access the file because it is being used by another process.")
                : Task.CompletedTask;
    }
}
