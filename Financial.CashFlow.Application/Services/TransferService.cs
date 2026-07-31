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

        var (sourceBank, destinationBank) = ResolveBanks(request.SourceBank, request.DestinationBank);

        var transfer = Transfer.Create(request.Date, sourceBank, destinationBank, request.Amount, request.Note);
        _repository.AddTransfer(transfer);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(transfer);
    }

    public async Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = FindTransferOrThrow(id);
        var (sourceBank, destinationBank) = ResolveBanks(request.SourceBank, request.DestinationBank);

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

    public IReadOnlyList<TransferDTO> GetTransfersByBank(string bankName) =>
        _repository.GetTransfers()
            .Where(t =>
                string.Equals(t.SourceBank, bankName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.DestinationBank, bankName, StringComparison.OrdinalIgnoreCase))
            .Select(ToDto)
            .ToList();

    private Transfer FindTransferOrThrow(Guid id) =>
        _repository.GetTransfers().FirstOrDefault(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Transfer '{id}' was not found.");

    private (string SourceBank, string DestinationBank) ResolveBanks(string sourceBank, string destinationBank)
    {
        var banks = _repository.GetBanks();

        if (!BankNameResolver.TryResolve(sourceBank, banks, out var resolvedSource))
        {
            throw new ArgumentException($"Bank '{sourceBank}' was not found.");
        }

        if (!BankNameResolver.TryResolve(destinationBank, banks, out var resolvedDestination))
        {
            throw new ArgumentException($"Bank '{destinationBank}' was not found.");
        }

        return (resolvedSource!.Name, resolvedDestination!.Name);
    }

    private static TransferDTO ToDto(Transfer transfer) => new()
    {
        Id = transfer.Id,
        Date = transfer.Date,
        SourceBank = transfer.SourceBank,
        DestinationBank = transfer.DestinationBank,
        Amount = transfer.Amount,
        Note = transfer.Note
    };
}
