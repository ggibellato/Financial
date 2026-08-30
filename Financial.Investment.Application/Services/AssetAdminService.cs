using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class AssetAdminService : IAssetAdminService
{
    private const string EntityType = "Asset";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AssetAdminService> _logger;

    public AssetAdminService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<AssetAdminService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<AssetAdminDTO> GetAssets()
    {
        using var span = StartSpan("GetAssets");
        try
        {
            var investments = _repository.GetInvestments();
            var result = investments.ActiveBrokers
                .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Select(a => ToDto(a, b, p, "Active"))))
                .Concat(investments.HistoricBrokers
                    .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Select(a => ToDto(a, b, p, "Historic")))))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAssets");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetAdminDTO> CreateAssetAsync(AssetAdminCreateDTO request)
    {
        using var span = StartSpan("CreateAsset");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var brokerName = Required(request.BrokerName, nameof(request.BrokerName));
            var portfolioName = Required(request.PortfolioName, nameof(request.PortfolioName));
            var name = Required(request.Name, nameof(request.Name));
            ValidateIsin(request.ISIN);

            var assetClass = request.Class ?? GlobalAssetClassMapping.Resolve(request.Country, request.LocalTypeCode);

            Broker? broker = null;
            Portfolio? portfolio = null;
            Asset? created = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                broker = _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(candidate => candidate.Name == brokerName)
                    ?? throw new KeyNotFoundException($"Active broker \"{brokerName}\" was not found.");
                portfolio = broker.FindPortfolio(portfolioName)
                    ?? throw new KeyNotFoundException($"Portfolio \"{portfolioName}\" was not found under broker \"{brokerName}\".");

                created = Asset.Create(name, request.ISIN, request.Exchange, request.Ticker, request.Country, request.LocalTypeCode, assetClass);
                portfolio.RegisterAsset(created);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateAsset");
            return ToDto(created!, broker!, portfolio!, "Active");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetAdminDTO> UpdateAssetAsync(string brokerName, string portfolioName, string currentName, AssetAdminUpdateDTO request)
    {
        using var span = StartSpan("UpdateAsset");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var requiredBrokerName = Required(brokerName, nameof(brokerName));
            var requiredPortfolioName = Required(portfolioName, nameof(portfolioName));
            Required(currentName, nameof(currentName));
            var newName = Required(request.Name, nameof(request.Name));
            ValidateIsin(request.ISIN);

            Broker? broker = null;
            Portfolio? portfolio = null;
            Asset? updated = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                var investments = _repository.GetInvestments();
                broker = investments.FindActiveBroker(requiredBrokerName) ?? investments.FindHistoricBroker(requiredBrokerName)
                    ?? throw new KeyNotFoundException($"Broker \"{requiredBrokerName}\" was not found.");
                portfolio = broker.FindPortfolio(requiredPortfolioName)
                    ?? throw new KeyNotFoundException($"Portfolio \"{requiredPortfolioName}\" was not found under broker \"{requiredBrokerName}\".");

                updated = portfolio.UpdateAssetIdentity(
                    currentName, newName, request.ISIN, request.Exchange, request.Ticker,
                    request.Country, request.LocalTypeCode, request.Class);
                return true;
            }).ConfigureAwait(false);

            var status = _repository.GetBrokerList(InvestmentScope.Active).Any(b => b.Name == broker!.Name) ? "Active" : "Historic";

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateAsset");
            return ToDto(updated!, broker!, portfolio!, status);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static void ValidateIsin(string? isin)
    {
        if (!IsinValidator.IsValid(isin))
        {
            throw new ArgumentException($"\"{isin}\" is not a validly formatted ISIN.", nameof(isin));
        }
    }

    private static string Required(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{parameterName} is required.", parameterName)
            : value;

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(AssetAdminService), operationName, EntityType);
    }

    private static AssetAdminDTO ToDto(Asset asset, Broker broker, Portfolio portfolio, string brokerStatus) => new()
    {
        Name = asset.Name,
        BrokerName = broker.Name,
        PortfolioName = portfolio.Name,
        BrokerStatus = brokerStatus,
        ISIN = asset.ISIN,
        Exchange = asset.Exchange,
        Ticker = asset.Ticker,
        Country = asset.Country,
        LocalTypeCode = asset.LocalTypeCode,
        Class = asset.Class,
        Quantity = asset.Quantity
    };
}
