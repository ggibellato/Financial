using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentSnapshotService : IInvestmentSnapshotService
{
    private readonly ICashFlowRepository _repository;

    public InvestmentSnapshotService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<InvestmentSnapshotDTO>> GetSnapshotsForMonthAsync(int year, int month)
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

        return existingSnapshots.Select(ToDto).ToList();
    }

    public async Task<InvestmentSnapshotDTO> UpdateSnapshotValueAsync(Guid id, UpdateInvestmentSnapshotValueDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Value < 0)
        {
            throw new ArgumentException("Value must not be negative.");
        }

        var snapshot = _repository.GetInvestmentSnapshots().FirstOrThrow(s => s.Id == id, "Investment snapshot", id);

        snapshot.Update(request.Value);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(snapshot);
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
