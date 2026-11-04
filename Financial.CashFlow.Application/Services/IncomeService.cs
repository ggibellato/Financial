using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeService : IIncomeService
{
    private const string EntityType = "Income";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<IncomeService> _logger;

    public IncomeService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<IncomeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request)
    {
        using var span = StartSpan("AddIncome");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (incomeSource, bank, potsAccount) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description, request.PotsAccountId);

            var income = Income.Create(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description, potsAccount);
            var deposit = BuildDeposit(income, request.FundingLines, Guid.NewGuid());

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddIncome(income);
                AddRecords(deposit);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, income.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddIncome");
            return ToDtoWithDeposit(income);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        using var span = StartSpan("UpdateIncome");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var income = FindIncomeOrThrow(id);

            var (incomeSource, bank, potsAccount) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description, request.PotsAccountId);

            var oldRecords = RecordsOf(income.Id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                income.UpdateDetails(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description, potsAccount);
                var newRecords = Rebuild(income, oldRecords, request.FundingLines);
                RemoveRecords(oldRecords);
                AddRecords(newRecords);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateIncome");
            return ToDtoWithDeposit(income);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteIncomeAsync(Guid id)
    {
        using var span = StartSpan("DeleteIncome");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindIncomeOrThrow(id);

            var records = RecordsOf(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                RemoveRecords(records);
                _repository.DeleteIncome(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteIncome");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<IncomeDTO> GetIncomesByMonth(int year, int month)
    {
        using var span = StartSpan("GetIncomesByMonth");
        try
        {
            var result = _repository.GetIncomes()
                .Where(i => i.Date.Year == year && i.Date.Month == month)
                .Select(ToDtoWithDeposit)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetIncomesByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(IncomeService), operationName, EntityType);
    }

    private Income FindIncomeOrThrow(Guid id) =>
        _repository.GetIncomes().FirstOrThrow(i => i.Id == id, "Income", id);

    private SplitRecords RecordsOf(Guid incomeId)
    {
        var transactions = _repository.GetPotTransactions().Where(transaction => transaction.Income?.Id == incomeId).ToList();
        var expenses = transactions.FirstOrDefault()?.OperationId is Guid operationId
            ? _repository.GetExpenses().Where(expense => expense.OperationId == operationId).ToList()
            : [];

        return new(transactions, expenses);
    }

    private SplitRecords Rebuild(Income income, SplitRecords oldRecords, IReadOnlyList<FundingLineDTO>? fundingLines)
    {
        var firstOld = oldRecords.Transactions.FirstOrDefault();
        return firstOld is { OperationId: null }
            ? new(OlderSplitTransactions(income), [])
            : BuildDeposit(income, fundingLines, firstOld?.OperationId ?? Guid.NewGuid());
    }

    private SplitRecords BuildDeposit(Income income, IReadOnlyList<FundingLineDTO>? fundingLines, Guid operationId)
    {
        if (income.PotsAccount is not { } potsAccount)
        {
            return new([], []);
        }

        var accounts = _repository.GetAccounts();
        var handSplitLines = (fundingLines ?? [])
            .Select(line => new PotFundingLine(CashFlowReferenceResolver.ResolvePaymentSource(accounts, line.BankId), line.Amount))
            .ToList();
        var deposit = PotDeposit.BuildFromIncome(income, potsAccount, handSplitLines, operationId);

        return new(deposit.Transactions, deposit.Expenses);
    }

    private static List<PotTransaction> OlderSplitTransactions(Income income)
    {
        var activePots = (income.PotsAccount?.Pots ?? []).Where(pot => pot.IsActive).ToList();
        var amounts = PotSplitAllocator.Allocate(activePots, PotDeposit.IncomeAmount(income.NetValue));

        return activePots
            .Select((pot, index) => PotTransaction.Create(pot, amounts[index], income.Date, income.Description ?? string.Empty, income))
            .ToList();
    }

    private void AddRecords(SplitRecords records)
    {
        foreach (var transaction in records.Transactions)
        {
            _repository.AddPotTransaction(transaction);
        }

        foreach (var expense in records.Expenses)
        {
            _repository.AddExpense(expense);
        }
    }

    private void RemoveRecords(SplitRecords records)
    {
        foreach (var transaction in records.Transactions)
        {
            _repository.DeletePotTransaction(transaction.Id);
        }

        foreach (var expense in records.Expenses)
        {
            _repository.DeleteExpense(expense.Id);
        }
    }

    private (IncomeSource IncomeSource, Account? Account, Account? PotsAccount) ValidateFields(Guid incomeSourceId, Guid? bankId, string? description, Guid? potsAccountId)
    {
        if (!EntityIdResolver.TryResolve(incomeSourceId, _repository.GetIncomeSources(), s => s.Id, out var resolvedIncomeSource))
        {
            throw new ArgumentException($"Income source '{incomeSourceId}' is not recognized.");
        }

        DescriptionValidator.EnsureWithinLimit(description);

        Account? resolvedBank = null;
        if (bankId is not null)
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetAccounts(), b => b.Id, out var bank))
            {
                throw new ArgumentException($"Bank '{bankId}' is not recognized.");
            }

            resolvedBank = bank!;
        }

        return (resolvedIncomeSource!, resolvedBank, CashFlowReferenceResolver.ResolvePotsAccount(_repository.GetAccounts(), potsAccountId));
    }

    private IncomeDTO ToDtoWithDeposit(Income income) => ToDto(income, RecordsOf(income.Id).Expenses);

    internal static IncomeDTO ToDto(Income income) => ToDto(income, []);

    private static IncomeDTO ToDto(Income income, IReadOnlyList<Expense> depositExpenses) => new()
    {
        Id = income.Id,
        Date = income.Date,
        IncomeSourceId = income.IncomeSource.Id,
        IncomeSourceName = income.IncomeSource.Name,
        GrossValue = income.GrossValue,
        NetValue = income.NetValue,
        BankId = income.Account?.Id,
        BankName = income.Account?.Name,
        Description = income.Description,
        PotsAccountId = income.PotsAccount?.Id,
        HasLinkedDeposit = depositExpenses.Count > 0,
        FundingLines = depositExpenses
            .Select(expense => new FundingLineDTO { BankId = expense.PaymentSourceAccount!.Id, Amount = expense.Value })
            .ToList()
    };

    private sealed record SplitRecords(IReadOnlyList<PotTransaction> Transactions, IReadOnlyList<Expense> Expenses);
}
