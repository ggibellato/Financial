using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentSnapshotService : IInvestmentSnapshotService
{
    private static readonly InvestmentAccount[] AllAccounts = Enum.GetValues<InvestmentAccount>();

    private readonly ICashFlowRepository _repository;

    public InvestmentSnapshotService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<InvestmentSnapshotDTO>> GetSnapshotsForMonthAsync(int year, int month)
    {
        var existingSnapshots = _repository.GetInvestmentSnapshots()
            .Where(s => s.Year == year && s.Month == month)
            .ToList();

        var created = false;
        foreach (var account in AllAccounts)
        {
            if (existingSnapshots.Any(s => s.Account == account))
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

        var snapshot = _repository.GetInvestmentSnapshots().FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Investment snapshot '{id}' was not found.");

        snapshot.Update(request.Value);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(snapshot);
    }

    private static InvestmentSnapshotDTO ToDto(InvestmentSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Account = snapshot.Account.ToString(),
        IsLiability = InvestmentAccountClassification.IsLiability(snapshot.Account),
        Year = snapshot.Year,
        Month = snapshot.Month,
        Value = snapshot.Value
    };
}
