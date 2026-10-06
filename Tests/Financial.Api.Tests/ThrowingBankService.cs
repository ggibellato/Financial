using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

internal sealed class ThrowingBankService(Func<Exception> exceptionFactory) : IBankService
{
    public static Action<IServiceCollection> Registering(Func<Exception> exceptionFactory) => services =>
    {
        services.RemoveAll<IBankService>();
        services.AddSingleton<IBankService>(new ThrowingBankService(exceptionFactory));
    };

    public IReadOnlyList<BankDTO> GetBanks() => throw exceptionFactory();

    public Task<BankDTO> CreateBankAsync(BankCreateDTO request) => throw new NotSupportedException();

    public Task<BankDTO> UpdateBankAsync(Guid id, BankUpdateDTO request) => throw new NotSupportedException();

    public Task DeleteBankAsync(Guid id) => throw new NotSupportedException();

    public Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request) => throw new NotSupportedException();

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => throw new NotSupportedException();

    public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => throw new NotSupportedException();
}
