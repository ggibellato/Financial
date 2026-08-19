using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentSnapshotService : IInvestmentSnapshotService
{
    private const string EntityType = "InvestmentSnapshot";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public InvestmentSnapshotService(ICashFlowRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public async Task<IReadOnlyList<InvestmentSnapshotDTO>> GetSnapshotsForMonthAsync(int year, int month)
    {
        using var span = StartSpan("GetSnapshotsForMonth");
        try
        {
            var accounts = _repository.GetInvestmentAccounts().ToList();
            var allSnapshots = _repository.GetInvestmentSnapshots().ToList();
            var scopedAccounts = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, allSnapshots, year, DateTime.Now.Year);
            var scopedIds = scopedAccounts.Select(a => a.Id).ToHashSet();

            var existingSnapshots = allSnapshots
                .Where(s => s.Year == year && s.Month == month && scopedIds.Contains(s.Account.Id))
                .ToList();

            var created = false;
            foreach (var account in scopedAccounts)
            {
                if (existingSnapshots.Any(s => s.Account.Id == account.Id))
                {
                    continue;
                }

                var snapshot = InvestmentSnapshot.Create(account, year, month, 0m);
                _repository.AddInvestmentSnapshot(snapshot);
                existingSnapshots.Add(snapshot);
                created = true;
            }

            if (created)
            {
                await _repository.SaveChangesAsync().ConfigureAwait(false);
            }

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return existingSnapshots.Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<InvestmentSnapshotDTO> UpdateSnapshotValueAsync(Guid id, UpdateInvestmentSnapshotValueDTO request)
    {
        using var span = StartSpan("UpdateSnapshotValue");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Value < 0)
            {
                throw new ArgumentException("Value must not be negative.");
            }

            var snapshot = _repository.GetInvestmentSnapshots().FirstOrThrow(s => s.Id == id, "Investment snapshot", id);

            snapshot.Update(request.Value);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return ToDto(snapshot);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        var span = _tracer.StartSpan($"CashFlow.InvestmentSnapshotService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private static InvestmentSnapshotDTO ToDto(InvestmentSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        AccountId = snapshot.Account.Id,
        AccountName = snapshot.Account.Name,
        IsLiability = snapshot.Account.IsLiability,
        Year = snapshot.Year,
        Month = snapshot.Month,
        Value = snapshot.Value
    };
}
