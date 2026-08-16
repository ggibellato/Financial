using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class BalanceAdjustmentService : IBalanceAdjustmentService
{
    private readonly ICashFlowRepository _repository;
    private readonly IBankService _bankService;

    public BalanceAdjustmentService(ICashFlowRepository repository, IBankService bankService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
    }

    public async Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid bankId, BalanceAdjustmentCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankId);
        var currentBalance = _bankService.GetBankBalanceAsOf(bank.Id, request.Date);
        var delta = request.TargetBalance - currentBalance;

        var adjustment = BalanceAdjustment.Create(request.Date, bank, request.TargetBalance, delta, request.Note);
        _repository.AddBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid bankId, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankId);
        var adjustment = FindAdjustmentOrThrow(bank, id);
        var currentBalance = _bankService.GetBankBalanceAsOf(bank.Id, request.Date, excludingAdjustmentId: id);
        var delta = request.TargetBalance - currentBalance;

        adjustment.UpdateDetails(request.Date, request.TargetBalance, delta, request.Note);
        _repository.UpdateBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task DeleteAdjustmentAsync(Guid bankId, Guid id)
    {
        var bank = ResolveBank(bankId);
        FindAdjustmentOrThrow(bank, id);

        _repository.DeleteBalanceAdjustment(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(Guid bankId)
    {
        if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
        {
            return Array.Empty<BalanceAdjustmentDTO>();
        }

        return _repository.GetBalanceAdjustments()
            .Where(a => a.Bank.Id == bank!.Id)
            .Select(ToDto)
            .ToList();
    }

    private Bank ResolveBank(Guid bankId)
    {
        if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
        {
            throw new ArgumentException($"Bank '{bankId}' was not found.");
        }

        return bank!;
    }

    private BalanceAdjustment FindAdjustmentOrThrow(Bank bank, Guid id) =>
        _repository.GetBalanceAdjustments()
            .FirstOrThrow(a => a.Id == id && a.Bank.Id == bank.Id, "Balance adjustment", id);

    private static BalanceAdjustmentDTO ToDto(BalanceAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        Date = adjustment.Date,
        BankId = adjustment.Bank.Id,
        BankName = adjustment.Bank.Name,
        TargetBalance = adjustment.TargetBalance,
        Delta = adjustment.Delta,
        Note = adjustment.Note
    };
}
