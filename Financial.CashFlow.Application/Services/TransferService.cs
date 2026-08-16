using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class TransferService : ITransferService
{
    private readonly ICashFlowRepository _repository;

    public TransferService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<TransferDTO> AddTransferAsync(TransferCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (sourceBank, destinationBank) = ResolveBanks(request.SourceBankId, request.DestinationBankId);

        var transfer = Transfer.Create(request.Date, sourceBank, destinationBank, request.Amount, request.Note);
        _repository.AddTransfer(transfer);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(transfer);
    }

    public async Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = FindTransferOrThrow(id);
        var (sourceBank, destinationBank) = ResolveBanks(request.SourceBankId, request.DestinationBankId);

        transfer.UpdateDetails(request.Date, sourceBank, destinationBank, request.Amount, request.Note);
        _repository.UpdateTransfer(transfer);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(transfer);
    }

    public async Task DeleteTransferAsync(Guid id)
    {
        FindTransferOrThrow(id);

        _repository.DeleteTransfer(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<TransferDTO> GetTransfersByMonth(int year, int month) =>
        _repository.GetTransfers()
            .Where(t => t.Date.Year == year && t.Date.Month == month)
            .Select(ToDto)
            .ToList();

    public IReadOnlyList<TransferDTO> GetTransfersByBank(Guid bankId)
    {
        if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
        {
            return Array.Empty<TransferDTO>();
        }

        return _repository.GetTransfers()
            .Where(t => t.SourceBank.Id == bank!.Id || t.DestinationBank.Id == bank.Id)
            .Select(ToDto)
            .ToList();
    }

    private Transfer FindTransferOrThrow(Guid id) =>
        _repository.GetTransfers().FirstOrThrow(t => t.Id == id, "Transfer", id);

    private (Bank SourceBank, Bank DestinationBank) ResolveBanks(Guid sourceBankId, Guid destinationBankId)
    {
        var banks = _repository.GetBanks();

        if (!EntityIdResolver.TryResolve(sourceBankId, banks, b => b.Id, out var resolvedSource))
        {
            throw new ArgumentException($"Bank '{sourceBankId}' was not found.");
        }

        if (!EntityIdResolver.TryResolve(destinationBankId, banks, b => b.Id, out var resolvedDestination))
        {
            throw new ArgumentException($"Bank '{destinationBankId}' was not found.");
        }

        return (resolvedSource!, resolvedDestination!);
    }

    private static TransferDTO ToDto(Transfer transfer) => new()
    {
        Id = transfer.Id,
        Date = transfer.Date,
        SourceBankId = transfer.SourceBank.Id,
        SourceBankName = transfer.SourceBank.Name,
        DestinationBankId = transfer.DestinationBank.Id,
        DestinationBankName = transfer.DestinationBank.Name,
        Amount = transfer.Amount,
        Note = transfer.Note
    };
}
