using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveService : IReserveService
{
    private const string IncomeSplitDescription = "Monthly income split";

    private readonly ICashFlowRepository _repository;

    public ReserveService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateNonNegative(request.GleisonSalaryGross, nameof(request.GleisonSalaryGross));
        ValidateNonNegative(request.GleisonSalaryNet, nameof(request.GleisonSalaryNet));
        ValidateNonNegative(request.ArianaSalaryGross, nameof(request.ArianaSalaryGross));
        ValidateNonNegative(request.ArianaSalaryNet, nameof(request.ArianaSalaryNet));
        ValidateNonNegative(request.Lottery, nameof(request.Lottery));
        ValidateNonNegative(request.DividendoJuros, nameof(request.DividendoJuros));

        var split = ReserveSplitCalculator.Calculate(
            request.GleisonSalaryNet, request.ArianaSalaryNet, request.Lottery, request.DividendoJuros);

        var movements = new[]
        {
            ReserveMovement.Create(ReserveBucket.Investimento, split.Investimento, request.Date, IncomeSplitDescription),
            ReserveMovement.Create(ReserveBucket.HouseTreats, split.HouseTreats, request.Date, IncomeSplitDescription),
            ReserveMovement.Create(ReserveBucket.Ariana, split.Ariana, request.Date, IncomeSplitDescription),
            ReserveMovement.Create(ReserveBucket.Gleison, split.Gleison, request.Date, IncomeSplitDescription)
        };

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

        return new IncomeSplitResultDTO
        {
            Dizimo = split.Dizimo,
            Investimento = split.Investimento,
            HouseTreats = split.HouseTreats,
            Ariana = split.Ariana,
            Gleison = split.Gleison
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

        if (!ReserveBucketParser.TryParse(request.Bucket, out var bucket))
        {
            throw new ArgumentException($"Bucket '{request.Bucket}' is not recognized.");
        }

        var currentBalance = GetBalance(bucket);
        if (request.Amount > currentBalance && !request.Confirmed)
        {
            throw new OverdraftConfirmationRequiredException(
                $"This withdrawal exceeds {bucket}'s balance of {currentBalance:F2}. Set confirmed=true to proceed.");
        }

        var movement = ReserveMovement.Create(bucket, -request.Amount, request.Date, request.Description);
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

    public IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances() =>
        Enum.GetValues<ReserveBucket>()
            .Select(bucket => new ReserveBucketBalanceDTO
            {
                Bucket = bucket.ToString(),
                Balance = GetBalance(bucket)
            })
            .ToList();

    public IReadOnlyList<ReserveMovementDTO> GetMovementHistory() =>
        _repository.GetReserveMovements()
            .OrderBy(m => m.Date)
            .Select(ToDto)
            .ToList();

    private decimal GetBalance(ReserveBucket bucket) =>
        _repository.GetReserveMovements().Where(m => m.Bucket == bucket).Sum(m => m.Amount);

    private static void ValidateNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{fieldName} must not be negative.");
        }
    }

    private static ReserveMovementDTO ToDto(ReserveMovement movement) => new()
    {
        Id = movement.Id,
        Bucket = movement.Bucket.ToString(),
        Amount = movement.Amount,
        Date = movement.Date,
        Description = movement.Description
    };
}
