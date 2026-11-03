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

            var operationId = Guid.NewGuid();
            var records = BuildDeposit(potsAccountId, request, operationId);

            await _repository.ApplyAndSaveAsync(() =>
            {
                AddRecords(records.Movements, records.BankExpenses);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Deposit");
            return ToDto(OperationOf(operationId, records.Movements, records.BankExpenses));
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

            var operation = FindOperationOfKind(operationId, PotOperationKind.Deposit, potsAccountId);
            EnsureNotAnIncomeDeposit(operation);
            var records = BuildDeposit(potsAccountId, request, operationId);

            await _repository.ApplyAndSaveAsync(() =>
            {
                RemoveRecords(operation);
                AddRecords(records.Movements, records.BankExpenses);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateDeposit");
            return ToDto(OperationOf(operationId, records.Movements, records.BankExpenses));
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

            var operationId = Guid.NewGuid();
            var records = BuildWithdrawal(potsAccountId, request, operationId, _repository.GetReserveMovements());

            await _repository.ApplyAndSaveAsync(() =>
            {
                AddRecords([records.Movement], records.BankExpenses);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, operationId.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Withdraw");
            return ToDto(OperationOf(operationId, [records.Movement], records.BankExpenses));
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

            var operation = FindOperationOfKind(operationId, PotOperationKind.Withdrawal, potsAccountId);
            var otherMovements = _repository.GetReserveMovements().Where(movement => movement.OperationId != operationId);
            var records = BuildWithdrawal(potsAccountId, request, operationId, otherMovements);

            await _repository.ApplyAndSaveAsync(() =>
            {
                RemoveRecords(operation);
                AddRecords([records.Movement], records.BankExpenses);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateWithdrawal");
            return ToDto(OperationOf(operationId, [records.Movement], records.BankExpenses));
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

    internal static (Account? Bank, Category? Category) ResolveWithdrawalBank(
        Guid? bankId, Guid? categoryId, string description, IReadOnlyList<Account> accounts, IEnumerable<Category> categories)
    {
        if (bankId is null && categoryId is null)
        {
            return (null, null);
        }

        if (bankId is null)
        {
            throw new ArgumentException("A bank is required when an expense category is given.");
        }

        if (categoryId is null)
        {
            throw new ArgumentException("Category is required when a bank is selected.");
        }

        DescriptionValidator.EnsureWithinLimit(description);

        var bank = CashFlowReferenceResolver.ResolvePaymentSource(accounts, bankId.Value);
        var category = CashFlowReferenceResolver.ResolveActiveCategory(categories, categoryId.Value);
        PotWithdrawal.EnsureUsableCategory(category, accounts);

        return (bank, category);
    }

    internal static void EnsureWithinBalance(ReserveBucket pot, decimal amount, bool confirmed, IEnumerable<ReserveMovement> movements)
    {
        var balance = AccountBalanceRule.PotBalance(pot, DateOnly.MaxValue, movements);
        if (amount > balance && !confirmed)
        {
            throw new OverdraftConfirmationRequiredException(
                FormattableString.Invariant($"This withdrawal exceeds {pot.Name}'s balance of {balance:F2}. Set confirmed=true to proceed."));
        }
    }

    private PotWithdrawalRecords BuildWithdrawal(
        Guid potsAccountId, PotWithdrawalRequestDTO request, Guid operationId, IEnumerable<ReserveMovement> balanceMovements)
    {
        EnsureDescriptionAndDate(request.Description, request.Date);

        var accounts = _repository.GetAccounts().ToList();
        var potsAccount = FindPotsAccount(accounts, potsAccountId);
        var pot = FindPot(potsAccount, request.PotId);
        var (bank, category) = ResolveWithdrawalBank(request.BankId, request.CategoryId, request.Description, accounts, _repository.GetCategories());
        EnsureWithinBalance(pot, request.Amount, request.Confirmed, balanceMovements);

        return PotWithdrawal.Build(potsAccount, pot, request.Amount, request.Date, request.Description, bank, category, operationId);
    }

    private PotDepositRecords BuildDeposit(Guid potsAccountId, PotDepositRequestDTO request, Guid operationId)
    {
        EnsureDescriptionAndDate(request.Description, request.Date);

        var accounts = _repository.GetAccounts().ToList();
        var potsAccount = FindPotsAccount(accounts, potsAccountId);
        var pot = request.PotId is Guid potId ? FindPot(potsAccount, potId) : null;
        var fundingLines = request.FundingLines
            .Select(line => new PotFundingLine(CashFlowReferenceResolver.ResolvePaymentSource(accounts, line.BankId), line.Amount))
            .ToList();

        return PotDeposit.Build(potsAccount, request.Amount, request.Date, request.Description, pot, fundingLines, operationId);
    }

    private void EnsureDescriptionAndDate(string description, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (date > _timeProvider.GetLocalToday())
        {
            throw new ArgumentException("Date must not be in the future.");
        }
    }

    private static Account FindPotsAccount(IEnumerable<Account> accounts, Guid potsAccountId) =>
        accounts.FirstOrThrow(account => account.Id == potsAccountId && account.Pots is not null, "Pots account", potsAccountId);

    private static ReserveBucket FindPot(Account potsAccount, Guid potId) =>
        potsAccount.Pots!.FirstOrDefault(candidate => candidate.Id == potId)
            ?? throw new ArgumentException($"That pot isn't in {potsAccount.Name}.");

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
        PotOperation.Read(operationId, _repository.GetReserveMovements(), _repository.GetExpenses(), _repository.GetAccounts())
            ?? throw new KeyNotFoundException($"Operation '{operationId}' was not found.");

    private PotOperation OperationOf(Guid operationId, IReadOnlyList<ReserveMovement> movements, IReadOnlyList<Expense> expenses) =>
        PotOperation.Read(operationId, movements, expenses, _repository.GetAccounts())!;

    private void AddRecords(IEnumerable<ReserveMovement> movements, IEnumerable<Expense> expenses)
    {
        foreach (var movement in movements)
        {
            _repository.AddReserveMovement(movement);
        }

        foreach (var expense in expenses)
        {
            _repository.AddExpense(expense);
        }
    }

    private void RemoveRecords(PotOperation operation)
    {
        foreach (var movement in operation.Movements)
        {
            _repository.DeleteReserveMovement(movement.Id);
        }

        foreach (var expense in operation.Expenses)
        {
            _repository.DeleteExpense(expense.Id);
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(PotsService), operationName, EntityType);
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
