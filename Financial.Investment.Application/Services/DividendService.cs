using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Rules;
using Financial.Investment.Domain.ValueObjects;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class DividendService : IDividendService
{
    private const string EntityType = "Dividend";

    private readonly IDividendDataSource _dividendDataSource;
    private readonly IAssetSnapshotSource _snapshotSource;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<DividendService> _logger;

    public DividendService(IDividendDataSource dividendDataSource, IAssetSnapshotSource snapshotSource, ITelemetryTracer tracer, ILogger<DividendService> logger)
    {
        _dividendDataSource = dividendDataSource ?? throw new ArgumentNullException(nameof(dividendDataSource));
        _snapshotSource = snapshotSource ?? throw new ArgumentNullException(nameof(snapshotSource));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request)
    {
        using var span = StartSpan("GetDividendHistory");
        try
        {
            var values = LoadDividends(request);
            var result = MapToHistory(values);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetDividendHistory");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request)
    {
        using var span = StartSpan("GetDividendSummary");
        try
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

            var priceMax = DividendValuationRules.CalculatePriceMaxBuy(averageDividend);
            var discountPercent = DividendValuationRules.CalculateDiscountPercent(snapshot.Price, priceMax);
            var dividendYieldPercent = DividendValuationRules.CalculateDividendYieldPercent(averageDividend, snapshot.Price);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetDividendSummary");
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
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(DividendService), operationName, EntityType);
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

        return _dividendDataSource.GetDividends(request.Ticker);
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
