using System.Globalization;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentSnapshotService : IInvestmentSnapshotService
{
    private const string EntityType = "InvestmentSnapshot";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<InvestmentSnapshotService> _logger;

    public InvestmentSnapshotService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<InvestmentSnapshotService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            await _repository.ApplyAndSaveAsync(() =>
            {
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

                return created;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetSnapshotsForMonth");
            return existingSnapshots.Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<InvestmentSnapshotDTO> UpdateSnapshotValueAsync(Guid id, InvestmentSnapshotValueUpdateDTO request)
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

            await _repository.ApplyAndSaveAsync(() =>
            {
                snapshot.Update(request.Value);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateSnapshotValue");
            return ToDto(snapshot);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<InvestmentSnapshotSuggestionsDTO> GetSuggestionsForMonthAsync(int year, int month)
    {
        using var span = StartSpan("GetSuggestionsForMonth");
        try
        {
            var snapshotDtos = await GetSnapshotsForMonthAsync(year, month).ConfigureAwait(false);
            var snapshotsByAccountId = snapshotDtos.ToDictionary(s => s.AccountId);

            var accounts = _repository.GetInvestmentAccounts()
                .Where(a => snapshotsByAccountId.ContainsKey(a.Id))
                .ToList();

            var monthLabel = new DateOnly(year, month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture);

            var suggestions = new List<InvestmentSnapshotSuggestionDTO>();
            var notUpdated = new List<InvestmentSnapshotSuggestionSkippedDTO>();

            foreach (var account in accounts)
            {
                var snapshot = snapshotsByAccountId[account.Id];

                switch (account.Source)
                {
                    case InvestmentAccountSource.CreditCard:
                        var matchingExpenses = _repository.GetExpenses()
                            .Where(e => e.CreditCard?.Id == account.CreditCard!.Id
                                && e.InvoiceDate is not null
                                && e.InvoiceDate.Value.Year == year
                                && e.InvoiceDate.Value.Month == month)
                            .ToList();

                        if (matchingExpenses.Count == 0)
                        {
                            notUpdated.Add(new InvestmentSnapshotSuggestionSkippedDTO
                            {
                                AccountId = account.Id,
                                AccountName = account.Name,
                                Reason = "No statement for this month yet"
                            });
                            break;
                        }

                        suggestions.Add(new InvestmentSnapshotSuggestionDTO
                        {
                            SnapshotId = snapshot.Id,
                            AccountId = account.Id,
                            AccountName = account.Name,
                            CurrentValue = snapshot.Value,
                            SuggestedValue = matchingExpenses.Sum(e => e.Value),
                            SourceDescription = $"{account.CreditCard!.Name} — {monthLabel} statement"
                        });
                        break;

                    case InvestmentAccountSource.ReserveBucketsSum:
                        var asOfDate = ReserveBucketAsOfDateResolver.LastDayOfPriorMonth(year, month);
                        var total = ReserveBucketAsOfDateResolver.TotalBalanceAsOf(
                            _repository.GetReserveMovements().ToList(), asOfDate);
                        var asOfLabel = asOfDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                        suggestions.Add(new InvestmentSnapshotSuggestionDTO
                        {
                            SnapshotId = snapshot.Id,
                            AccountId = account.Id,
                            AccountName = account.Name,
                            CurrentValue = snapshot.Value,
                            SuggestedValue = total,
                            SourceDescription = $"Sum of reserve buckets — as of {asOfLabel}"
                        });
                        break;

                    case InvestmentAccountSource.None:
                    default:
                        break;
                }
            }

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetSuggestionsForMonth");
            return new InvestmentSnapshotSuggestionsDTO { Suggestions = suggestions, NotUpdated = notUpdated };
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
        return _tracer.StartServiceSpan("CashFlow", nameof(InvestmentSnapshotService), operationName, EntityType);
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
