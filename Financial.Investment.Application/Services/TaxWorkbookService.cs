using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;
using static Financial.Investment.Application.Validation.RequiredValueValidator;

namespace Financial.Investment.Application.Services;

public sealed class TaxWorkbookService : ITaxWorkbookService
{
    private const string EntityType = "TaxWorkbook";

    private static readonly IReadOnlyDictionary<CalculationStatus, int> StatusRank = new Dictionary<CalculationStatus, int>
    {
        [CalculationStatus.RequiresReview] = 0,
        [CalculationStatus.Incomplete] = 1,
        [CalculationStatus.Estimated] = 2,
        [CalculationStatus.Final] = 3
    };

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TaxWorkbookService> _logger;

    public TaxWorkbookService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<TaxWorkbookService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<TaxWorkbookOptionDTO> GetWorkbookOptions()
    {
        using var span = StartSpan("GetWorkbookOptions");
        try
        {
            var investments = _repository.GetInvestments();
            var result = AllAssets(investments)
                .SelectMany(asset => asset.TaxClassifications)
                .Where(c => c.Status == TaxClassificationStatus.Active)
                .Select(c => (c.Jurisdiction, c.TaxYear))
                .Distinct()
                .OrderBy(pair => pair.Jurisdiction)
                .ThenBy(pair => pair.TaxYear, StringComparer.Ordinal)
                .Select(pair => new TaxWorkbookOptionDTO { Jurisdiction = pair.Jurisdiction, TaxYear = pair.TaxYear })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetWorkbookOptions");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public TaxWorkbookDTO GetWorkbook(string jurisdiction, string taxYear)
    {
        using var span = StartSpan("GetWorkbook");
        try
        {
            var requiredTaxYear = Required(taxYear, nameof(taxYear));
            if (!EnumParser.TryParseEnum<Jurisdiction>(jurisdiction, out var parsedJurisdiction))
            {
                throw new ArgumentException($"\"{jurisdiction}\" is not a recognized jurisdiction.", nameof(jurisdiction));
            }

            var investments = _repository.GetInvestments();
            var entries = new List<TaxWorkbookEntryDTO>();

            foreach (var asset in AllAssets(investments))
            {
                foreach (var classification in asset.TaxClassifications)
                {
                    if (classification.Status != TaxClassificationStatus.Active ||
                        classification.Jurisdiction != parsedJurisdiction ||
                        classification.TaxYear != requiredTaxYear)
                    {
                        continue;
                    }

                    entries.Add(ToEntryDto(asset, classification, investments));
                }
            }

            var categoryTotals = entries
                .GroupBy(e => e.EventCategory)
                .Select(group => new TaxCategoryTotalDTO
                {
                    EventCategory = group.Key,
                    TotalProceeds = SumOrNull(group, e => e.Proceeds),
                    TotalCostBasis = SumOrNull(group, e => e.CostBasis),
                    TotalGainLoss = SumOrNull(group, e => e.GainLoss),
                    TotalGrossAmount = SumOrNull(group, e => e.GrossAmount),
                    TotalWithheldAmount = SumOrNull(group, e => e.WithheldAmount),
                    TotalNetAmount = SumOrNull(group, e => e.NetAmount)
                })
                .ToList();

            var result = new TaxWorkbookDTO
            {
                Jurisdiction = parsedJurisdiction,
                TaxYear = requiredTaxYear,
                Entries = entries,
                CategoryTotals = categoryTotals,
                CalculationStatus = entries.Count == 0
                    ? null
                    : entries.Select(e => e.CalculationStatus).MinBy(status => StatusRank[status])
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetWorkbook");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static TaxWorkbookEntryDTO ToEntryDto(Asset asset, TaxClassification classification, Investments investments)
    {
        var date = classification.SourceType == SourceType.Disposal
            ? asset.DisposalRecords.FirstOrDefault(r => r.Id == classification.SourceId)?.Date
            : asset.Credits.FirstOrDefault(c => c.Id == classification.SourceId)?.Date;

        var ruleLabel = classification.TaxRuleId is Guid ruleId
            ? investments.FindTaxRule(ruleId)?.Label
            : null;

        return new TaxWorkbookEntryDTO
        {
            Id = classification.Id,
            Date = date ?? default,
            EventCategory = classification.EventCategory,
            Proceeds = classification.Proceeds,
            CostBasis = classification.CostBasis,
            GainLoss = classification.GainLoss,
            GrossAmount = classification.GrossAmount,
            WithheldAmount = classification.WithheldAmount,
            NetAmount = classification.NetAmount,
            CalculationStatus = classification.CalculationStatus,
            EvidenceReference = classification.SourceId,
            TaxRuleLabel = ruleLabel
        };
    }

    private static decimal? SumOrNull(IEnumerable<TaxWorkbookEntryDTO> entries, Func<TaxWorkbookEntryDTO, decimal?> selector)
    {
        var values = entries.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count == 0 ? null : values.Sum();
    }

    private static IEnumerable<Asset> AllAssets(Investments investments) =>
        investments.ActiveBrokers.Concat(investments.HistoricBrokers)
            .SelectMany(broker => broker.Portfolios)
            .SelectMany(portfolio => portfolio.Assets);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(TaxWorkbookService), operationName, EntityType);
    }
}
