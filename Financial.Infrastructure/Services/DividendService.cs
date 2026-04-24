using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Common;
using Financial.Infrastructure.Integrations.WebPageParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Infrastructure.Services;

public sealed class DividendService : IDividendService
{
    private const decimal RequiredYield = 0.06m;

    public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request)
    {
        var values = LoadDividends(request);
        return values
            .OrderByDescending(dividend => dividend.Date)
            .Select(dividend => new DividendHistoryItemDTO
            {
                Type = dividend.Type.ToString(),
                Date = dividend.Date,
                Value = dividend.Value
            })
            .ToList();
    }

    public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request)
    {
        var values = LoadDividends(request);
        var snapshot = GoogleFinance.GetFinancialInfoSnapshot(request.Exchange, request.Ticker);
        var yearTotals = values
            .GroupBy(dividend => dividend.Date.Year)
            .Select(group => new DividendYearTotalDTO
            {
                Year = group.Key,
                Total = group.Sum(dividend => dividend.Value)
            })
            .OrderByDescending(group => group.Year)
            .ToList();

        var averageDividend = yearTotals
            .Where(total => total.Year < DateTime.Today.Year)
            .OrderByDescending(total => total.Year)
            .Take(5)
            .Select(total => total.Total)
            .DefaultIfEmpty(0m)
            .Average();

        var priceMax = averageDividend > 0m ? averageDividend / RequiredYield : 0m;
        var discountPercent = priceMax > 0m ? (1m - (snapshot.Price / priceMax)) * 100m : 0m;

        return new DividendSummaryDTO
        {
            Exchange = request.Exchange,
            Ticker = snapshot.Ticker,
            Name = snapshot.Name,
            CurrentPrice = snapshot.Price,
            PriceAsOf = snapshot.AsOf,
            AverageDividendLastFiveYears = averageDividend,
            PriceMaxBuy = priceMax,
            DiscountPercent = discountPercent,
            YearTotals = yearTotals
        };
    }

    private static IReadOnlyList<DividendValue> LoadDividends(DividendLookupRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Exchange) || string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Exchange and ticker are required.", nameof(request));
        }

        return DadosMercadoDividend.GetDividendInfo(request.Ticker);
    }
}
