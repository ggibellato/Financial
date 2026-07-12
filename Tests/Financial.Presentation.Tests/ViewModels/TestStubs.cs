using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Presentation.Tests.ViewModels;

/// <summary>
/// Shared test doubles for AssetDetailsViewModel tests, used across the broker summary,
/// portfolio summary, credits chart, and transactions chart test files.
/// </summary>
internal sealed class StubTransactionService : ITransactionService
{
    public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
}

internal sealed class StubCreditService : ICreditService
{
    public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
}

internal sealed class StubAssetPriceService : IAssetPriceService
{
    public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) =>
        new() { Exchange = request.Exchange, Ticker = request.Ticker, Price = 0m };
}

internal sealed class StubBrokerBreakdownService : IBrokerBreakdownService
{
    public IReadOnlyList<PortfolioBreakdownItemDTO> Breakdown { get; set; } = [];
    public Exception? ExceptionToThrow { get; set; }
    public string? LastBrokerName { get; private set; }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
    {
        LastBrokerName = brokerName;
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Breakdown;
    }
}

internal sealed class StubTransactionQueryService : ITransactionQueryService
{
    public IReadOnlyList<TransactionSummaryItemDTO> BrokerTransactions { get; set; } = [];
    public IReadOnlyList<TransactionSummaryItemDTO> PortfolioTransactions { get; set; } = [];
    public Exception? ExceptionToThrow { get; set; }
    public string? LastBrokerName { get; private set; }
    public string? LastPortfolioBrokerName { get; private set; }
    public string? LastPortfolioName { get; private set; }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName)
    {
        LastBrokerName = brokerName;
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return BrokerTransactions;
    }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName)
    {
        LastPortfolioBrokerName = brokerName;
        LastPortfolioName = portfolioName;
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return PortfolioTransactions;
    }
}
