using System;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Repositories;

public sealed class AssetPriceService : IAssetPriceService
{
    public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Exchange) || string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Exchange and ticker are required.", nameof(request));
        }

        var snapshot = GoogleFinance.GetFinancialInfoSnapshot(request.Exchange, request.Ticker);
        return new AssetPriceDTO
        {
            Exchange = request.Exchange,
            Ticker = snapshot.Ticker,
            Name = snapshot.Name,
            Price = snapshot.Price,
            AsOf = snapshot.AsOf
        };
    }
}
