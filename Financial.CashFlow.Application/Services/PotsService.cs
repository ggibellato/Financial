using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class PotsService : IPotsService
{
    private const string EntityType = "PotOperation";
    private const string AccountEntityType = "Account";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PotsService> _logger;

    public PotsService(ICashFlowRepository repository, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<PotsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PotOperationDTO> DepositAsync(Guid potsAccountId, PotDepositRequestDTO request)
    {
        using var span = StartSpan("Deposit");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var operation = BuildDeposit(potsAccountId, request, Guid.NewGuid());
            await SaveAsync(operation).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, operation.OperationId.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Deposit");
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<PotOperationDTO> UpdateDepositAsync(Guid potsAccountId, Guid operationId, PotDepositRequestDTO request)
    {
        using var span = StartSpan("UpdateDeposit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var existing = FindOperationOfKind(operationId, PotOperationKind.Deposit, potsAccountId);
            EnsureNotAnIncomeDeposit(existing);
            var operation = BuildDeposit(potsAccountId, request, operationId);
            await SaveAsync(operation, existing).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateDeposit");
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<PotOperationDTO> WithdrawAsync(Guid potsAccountId, PotWithdrawalRequestDTO request)
    {
        using var span = StartSpan("Withdraw");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var operation = BuildWithdrawal(potsAccountId, request, Guid.NewGuid(), _repository.GetPotTransactions());
            await SaveAsync(operation).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, operation.OperationId.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Withdraw");
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<PotOperationDTO> UpdateWithdrawalAsync(Guid potsAccountId, Guid operationId, PotWithdrawalRequestDTO request)
    {
        using var span = StartSpan("UpdateWithdrawal");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var existing = FindOperationOfKind(operationId, PotOperationKind.Withdrawal, potsAccountId);
            var otherTransactions = _repository.GetPotTransactions().Where(transaction => transaction.OperationId != operationId);
            var operation = BuildWithdrawal(potsAccountId, request, operationId, otherTransactions);
            await SaveAsync(operation, existing).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateWithdrawal");
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public PotOperationDTO GetOperation(Guid operationId)
    {
        using var span = StartSpan("GetOperation");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
        try
        {
            var result = ToDto(FindOperation(operationId));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetOperation");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteOperationAsync(Guid operationId)
    {
        using var span = StartSpan("DeleteOperation");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
        try
        {
            var operation = FindOperation(operationId);
            EnsureNotAnIncomeDeposit(operation);

            await _repository.ApplyAndSaveAsync(() =>
            {
                RemoveRecords(operation);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteOperation");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task ReplacePotsAsync(Guid potsAccountId, PotsUpdateDTO request)
    {
        using var span = StartSpan("ReplacePots", AccountEntityType);
        span.SetAttribute(TelemetryAttributeKeys.EntityId, potsAccountId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var potsAccount = FindPotsAccount(_repository.GetAccounts(), potsAccountId);
            var changes = request.Pots
                .Select(pot => new PotChange(pot.Id, pot.Name, pot.SplitPercentage, pot.IsActive))
                .ToList();
            if (Account.FindDuplicatePotName(changes) is string duplicate)
            {
                throw new DuplicateNameException(Account.DuplicatePotNameMessage(duplicate));
            }

            potsAccount.EnsureCanReplacePots(changes);

            await _repository.ApplyAndSaveAsync(() =>
            {
                potsAccount.ReplacePots(changes);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "ReplacePots");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public PotsSummaryDTO GetPots(Guid potsAccountId, int year, int month)
    {
        using var span = StartSpan("GetPots", AccountEntityType);
        span.SetAttribute(TelemetryAttributeKeys.EntityId, potsAccountId.ToString());
        try
        {
            var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var potsAccount = FindPotsAccount(_repository.GetAccounts(), potsAccountId);
            var transactions = _repository.GetPotTransactions().ToList();
            var pots = potsAccount.Pots!
                .OrderByDescending(pot => pot.IsActive)
                .ThenBy(pot => pot.Name, StringComparer.OrdinalIgnoreCase)
                .Select(pot => new PotBalanceDTO
                {
                    Id = pot.Id,
                    Name = pot.Name,
                    SplitPercentage = pot.SplitPercentage,
                    IsActive = pot.IsActive,
                    Balance = AccountBalanceRule.PotBalance(pot, endOfMonth, transactions)
                })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPots");
            return new PotsSummaryDTO
            {
                PotsAccountId = potsAccount.Id,
                Currency = potsAccount.Currency.ToString(),
                Total = pots.Sum(pot => pot.Balance),
                Pots = pots
            };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private PotOperation BuildWithdrawal(
        Guid potsAccountId, PotWithdrawalRequestDTO request, Guid operationId, IEnumerable<PotTransaction> balanceTransactions)
    {
        EnsureDescription(request.Description);

        var accounts = _repository.GetAccounts().ToList();
        var potsAccount = FindPotsAccount(accounts, potsAccountId);
        var pot = potsAccount.FindPot(request.PotId);
        var (bank, category) = PotWithdrawalValidator.ResolveBankAndCategory(
            request.BankId, request.CategoryId, request.Description, accounts, _repository.GetCategories());
        var operation = PotWithdrawal.Build(potsAccount, pot, request.Amount, request.Date, request.Description, bank, category, operationId, _timeProvider.GetLocalToday());
        PotWithdrawalValidator.EnsureWithinBalance(pot, request.Amount, request.Confirmed, balanceTransactions);

        return operation;
    }

    private PotOperation BuildDeposit(Guid potsAccountId, PotDepositRequestDTO request, Guid operationId)
    {
        EnsureDescription(request.Description);

        var accounts = _repository.GetAccounts().ToList();
        var potsAccount = FindPotsAccount(accounts, potsAccountId);
        var pot = request.PotId is Guid potId ? potsAccount.FindPot(potId) : null;
        var fundingLines = request.FundingLines
            .Select(line => new PotFundingLine(CashFlowReferenceResolver.ResolvePaymentSource(accounts, line.BankId), line.Amount))
            .ToList();

        return PotDeposit.Build(potsAccount, request.Amount, request.Date, request.Description, pot, fundingLines, operationId, _timeProvider.GetLocalToday());
    }

    private static void EnsureDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }
    }

    private static Account FindPotsAccount(IEnumerable<Account> accounts, Guid potsAccountId) =>
        accounts.FirstOrThrow(account => account.Id == potsAccountId && account.Pots is not null, "Pots account", potsAccountId);

    private static void EnsureNotAnIncomeDeposit(PotOperation operation)
    {
        if (operation.IncomeId is not null)
        {
            throw new LinkedOperationConflictException("This deposit belongs to an income; edit the income.");
        }
    }

    private PotOperation FindOperationOfKind(Guid operationId, PotOperationKind kind, Guid potsAccountId)
    {
        var operation = FindOperation(operationId);
        return operation.Kind == kind && operation.PotsAccount.Id == potsAccountId
            ? operation
            : throw new KeyNotFoundException($"{kind} '{operationId}' was not found.");
    }

    private PotOperation FindOperation(Guid operationId) =>
        PotOperation.Read(operationId, _repository.GetPotTransactions(), _repository.GetExpenses(), _repository.GetAccounts())
            ?? throw new KeyNotFoundException($"Operation '{operationId}' was not found.");

    private Task SaveAsync(PotOperation operation, PotOperation? replaced = null) =>
        _repository.ApplyAndSaveAsync(() =>
        {
            if (replaced is not null)
            {
                RemoveRecords(replaced);
            }

            AddRecords(operation);
            return true;
        });

    private void AddRecords(PotOperation operation)
    {
        foreach (var transaction in operation.Transactions)
        {
            _repository.AddPotTransaction(transaction);
        }

        foreach (var expense in operation.Expenses)
        {
            _repository.AddExpense(expense);
        }
    }

    private void RemoveRecords(PotOperation operation)
    {
        foreach (var transaction in operation.Transactions)
        {
            _repository.DeletePotTransaction(transaction.Id);
        }

        foreach (var expense in operation.Expenses)
        {
            _repository.DeleteExpense(expense.Id);
        }
    }

    private ITelemetrySpan StartSpan(string operationName, string entityType = EntityType)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(PotsService), operationName, entityType);
    }

    private static PotOperationDTO ToDto(PotOperation operation) => new()
    {
        OperationId = operation.OperationId,
        Kind = operation.Kind.ToString(),
        PotsAccountId = operation.PotsAccount.Id,
        Date = operation.Date,
        Description = operation.Description,
        Amount = operation.Amount,
        Into = operation.Into?.ToString(),
        PotId = operation.Pot?.Id,
        FundingLines = operation.FundingLines
            .Select(line => new FundingLineDTO { BankId = line.Bank.Id, Amount = line.Amount })
            .ToList(),
        BankId = operation.Bank?.Id,
        CategoryId = operation.Category?.Id,
        IncomeId = operation.IncomeId
    };
}
