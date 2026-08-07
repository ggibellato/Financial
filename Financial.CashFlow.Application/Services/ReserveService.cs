using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveService : IReserveService
{
    // The 4 buckets seeded by ReserveBucketMigrator. Resolving them by name here (rather than
    // switching to _repository.GetReserveBuckets() for iteration) keeps this feature's behavior
    // byte-identical to before - iterating whatever buckets the repository holds is a later,
    // separate change.
    private static readonly string[] CanonicalBucketNames = ["Investimento", "HouseTreats", "Ariana", "Gleison"];

    private readonly ICashFlowRepository _repository;

    public ReserveService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.", nameof(request.Description));
        }

        var activeBuckets = _repository.GetReserveBuckets().Where(b => b.IsActive).ToList();
        if (activeBuckets.Count == 0)
        {
            throw new ArgumentException("No reserve bucket is currently active.");
        }

        var movements = activeBuckets
            .Select(bucket => ReserveMovement.Create(bucket, bucket.CalculateSplitAmount(request.Amount), request.Date, request.Description))
            .ToList();

        foreach (var movement in movements)
        {
            _repository.AddReserveMovement(movement);
        }

        try
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
        }
        catch
        {
            foreach (var movement in movements)
            {
                _repository.DeleteReserveMovement(movement.Id);
            }

            throw;
        }

        var splitAmounts = movements
            .Select(movement => new BucketSplitAmountDTO { Bucket = movement.Bucket.Name, Amount = movement.Amount })
            .ToList();

        return new IncomeSplitResultDTO
        {
            Buckets = splitAmounts,
            Total = splitAmounts.Sum(b => b.Amount)
        };
    }

    public async Task<ReserveMovementDTO> PostWithdrawalAsync(WithdrawalRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (!ReserveBucketNameResolver.TryResolve(request.Bucket, _repository.GetReserveBuckets(), out var bucket))
        {
            throw new ArgumentException($"Bucket '{request.Bucket}' is not recognized.");
        }

        var currentBalance = GetBalance(bucket!);
        if (request.Amount > currentBalance && !request.Confirmed)
        {
            throw new OverdraftConfirmationRequiredException(
                $"This withdrawal exceeds {bucket!.Name}'s balance of {currentBalance:F2}. Set confirmed=true to proceed.");
        }

        var movement = ReserveMovement.Create(bucket!, -request.Amount, request.Date, request.Description);
        _repository.AddReserveMovement(movement);

        try
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
        }
        catch
        {
            _repository.DeleteReserveMovement(movement.Id);
            throw;
        }

        return ToDto(movement);
    }

    public IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances()
    {
        var balanceByBucket = _repository.GetReserveMovements()
            .GroupBy(m => m.Bucket)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Amount));

        var buckets = ResolveCanonicalBuckets();

        return CanonicalBucketNames
            .Select(name => buckets[name])
            .Select(bucket => new ReserveBucketBalanceDTO
            {
                Bucket = bucket.Name,
                Balance = balanceByBucket.GetValueOrDefault(bucket)
            })
            .ToList();
    }

    public IReadOnlyList<ReserveMovementDTO> GetMovementHistory() =>
        _repository.GetReserveMovements()
            .OrderByDescending(m => m.Date)
            .Select(ToDto)
            .ToList();

    public async Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, UpdateReserveMovementDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (!ReserveBucketNameResolver.TryResolve(request.Bucket, _repository.GetReserveBuckets(), out var bucket))
        {
            throw new ArgumentException($"Bucket '{request.Bucket}' is not recognized.");
        }

        var movement = _repository.GetReserveMovements().FirstOrDefault(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Reserve movement '{id}' was not found.");

        movement.Update(bucket!, request.Amount, request.Date, request.Description);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(movement);
    }

    public async Task DeleteMovementAsync(Guid id)
    {
        var movement = _repository.GetReserveMovements().FirstOrDefault(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Reserve movement '{id}' was not found.");

        // Movements from the same income split share Date+Description (see PostIncomeSplitAsync) -
        // deleting one deletes the whole split, not just this bucket's line.
        var group = _repository.GetReserveMovements()
            .Where(m => m.Date == movement.Date && m.Description == movement.Description)
            .ToList();

        foreach (var groupMovement in group)
        {
            _repository.DeleteReserveMovement(groupMovement.Id);
        }

        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    private decimal GetBalance(ReserveBucket bucket) =>
        _repository.GetReserveMovements().Where(m => m.Bucket == bucket).Sum(m => m.Amount);

    private Dictionary<string, ReserveBucket> ResolveCanonicalBuckets()
    {
        var buckets = _repository.GetReserveBuckets();
        var resolved = new Dictionary<string, ReserveBucket>();

        foreach (var name in CanonicalBucketNames)
        {
            if (!ReserveBucketNameResolver.TryResolve(name, buckets, out var bucket))
            {
                throw new InvalidOperationException($"Reserve bucket '{name}' is not seeded.");
            }

            resolved[name] = bucket!;
        }

        return resolved;
    }

    private static ReserveMovementDTO ToDto(ReserveMovement movement) => new()
    {
        Id = movement.Id,
        Bucket = movement.Bucket.Name,
        Amount = movement.Amount,
        Date = movement.Date,
        Description = movement.Description
    };
}
