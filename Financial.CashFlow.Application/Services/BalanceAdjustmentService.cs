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

    public async Task<BalanceAdjustmentDTO> AddAdjustmentAsync(string bankName, BalanceAdjustmentCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankName);
        var currentBalance = _bankService.GetBankBalanceAsOf(bank.Name, request.Date);
        var delta = request.TargetBalance - currentBalance;

        var adjustment = BalanceAdjustment.Create(request.Date, bank, request.TargetBalance, delta, request.Note);
        _repository.AddBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(string bankName, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankName);
        var adjustment = FindAdjustmentOrThrow(bank, id);
        var currentBalance = _bankService.GetBankBalanceAsOf(bank.Name, request.Date, excludingAdjustmentId: id);
        var delta = request.TargetBalance - currentBalance;

        adjustment.UpdateDetails(request.Date, request.TargetBalance, delta, request.Note);
        _repository.UpdateBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task DeleteAdjustmentAsync(string bankName, Guid id)
    {
        var bank = ResolveBank(bankName);
        FindAdjustmentOrThrow(bank, id);

        _repository.DeleteBalanceAdjustment(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(string bankName)
    {
        if (!BankNameResolver.TryResolve(bankName, _repository.GetBanks(), out var bank))
        {
            return Array.Empty<BalanceAdjustmentDTO>();
        }

        return _repository.GetBalanceAdjustments()
            .Where(a => a.Bank.Id == bank!.Id)
            .Select(ToDto)
            .ToList();
    }

    private Bank ResolveBank(string bankName)
    {
        if (!BankNameResolver.TryResolve(bankName, _repository.GetBanks(), out var bank))
        {
            throw new ArgumentException($"Bank '{bankName}' was not found.");
        }

        return bank!;
    }

    private BalanceAdjustment FindAdjustmentOrThrow(Bank bank, Guid id) =>
        _repository.GetBalanceAdjustments()
            .FirstOrDefault(a => a.Id == id && a.Bank.Id == bank.Id)
        ?? throw new KeyNotFoundException($"Balance adjustment '{id}' was not found.");

    private static BalanceAdjustmentDTO ToDto(BalanceAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        Date = adjustment.Date,
        Bank = adjustment.Bank.Name,
        TargetBalance = adjustment.TargetBalance,
        Delta = adjustment.Delta,
        Note = adjustment.Note
    };
}
