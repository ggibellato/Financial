using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ITransferService
{
    Task<TransferDTO> AddTransferAsync(TransferCreateDTO request);
    Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request);
    Task DeleteTransferAsync(Guid id);
    IReadOnlyList<TransferDTO> GetTransfersByMonth(int year, int month);
    IReadOnlyList<TransferDTO> GetTransfersByBank(string bankName);
}
