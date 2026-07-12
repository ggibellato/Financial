using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Rules;
using Financial.Domain.ValueObjects;

namespace Financial.Application.Services;

public sealed class DividendService : IDividendService
{
    private readonly IDividendDataSource _dividendDataSource;
    private readonly IAssetSnapshotSource _snapshotSource;

    public DividendService(IDividendDataSource dividendDataSource, IAssetSnapshotSource snapshotSource)
    {
        _dividendDataSource = dividendDataSource ?? throw new ArgumentNullException(nameof(dividendDataSource));
        _snapshotSource = snapshotSource ?? throw new ArgumentNullException(nameof(snapshotSource));
    }

    public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request)
    {
        var values = LoadDividends(request);
        return MapToHistory(values);
    }

    public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request)
    {
        var values = LoadDividends(request);
        var snapshot = _snapshotSource.GetSnapshot(request.Exchange, request.Ticker);

        var history = MapToHistory(values);

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
            .Take(DividendValuationRules.DividendYearsLookback)
            .Select(total => total.Total)
            .DefaultIfEmpty(0m)
            .Average();

        var priceMax = averageDividend > 0m ? averageDividend / DividendValuationRules.RequiredYield : 0m;
        var discountPercent = priceMax > 0m ? (1m - (snapshot.Price / priceMax)) * 100m : 0m;
        var dividendYieldPercent = snapshot.Price > 0m ? (averageDividend / snapshot.Price) * 100m : 0m;

        return new DividendSummaryDTO
        {
            Exchange = request.Exchange,
            Ticker = snapshot.Ticker,
            Name = snapshot.Name,
            CurrentPrice = snapshot.Price,
            PriceAsOf = snapshot.AsOf,
            AverageDividendLastFiveYears = averageDividend,
            DividendYieldPercent = dividendYieldPercent,
            PriceMaxBuy = priceMax,
            DiscountPercent = discountPercent,
            History = history,
            YearTotals = yearTotals
        };
    }

    private IReadOnlyList<DividendValue> LoadDividends(DividendLookupRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Exchange) || string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Exchange and ticker are required.", nameof(request));
        }

        return _dividendDataSource.GetDividends(request.Exchange, request.Ticker);
    }

    private static List<DividendHistoryItemDTO> MapToHistory(IReadOnlyList<DividendValue> values)
    {
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
}
