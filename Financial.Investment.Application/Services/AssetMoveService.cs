using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class AssetMoveService : IAssetMoveService
{
    private const string EntityType = "Asset";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AssetMoveService> _logger;

    public AssetMoveService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        ITelemetryTracer tracer,
        ILogger<AssetMoveService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request)
    {
        using var span = StartSpan("MoveAsset");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var brokerName = Required(request.BrokerName, nameof(request.BrokerName));
            var sourcePortfolioName = Required(request.SourcePortfolioName, nameof(request.SourcePortfolioName));
            var assetName = Required(request.AssetName, nameof(request.AssetName));
            var destinationPortfolioName = Required(request.DestinationPortfolioName, nameof(request.DestinationPortfolioName));
            var scope = ParseScope(request.Scope);

            var broker = FindBroker(brokerName, scope);

            // The move runs inside the save rather than before it: the whole document is
            // re-serialized on write, and a change applied outside that exclusion can be walked
            // half-applied. Validating out here and mutating in there would also let a concurrent
            // write invalidate the check between the two, so the domain call - which validates and
            // mutates together - is the entire delegate.
            await _repository.ApplyAndSaveAsync(() =>
            {
                broker.MoveAsset(sourcePortfolioName, assetName, destinationPortfolioName);
                return true;
            }).ConfigureAwait(false);

            // The domain trims the destination name, so the asset is read back from where it
            // actually landed rather than from what was asked for.
            var landedIn = broker.FindPortfolio(destinationPortfolioName.Trim())!.Name;
            var asset = _navigationService.GetAssetDetails(brokerName, landedIn, assetName, scope)
                ?? throw new KeyNotFoundException($"Asset \"{assetName}\" could not be read back from \"{landedIn}\".");

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "MoveAsset");
            return asset;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    /// <summary>
    /// Both ends of a move sit under one broker: a broker carries its own currency and its own
    /// reporting, so relocating an asset across brokers is a different operation with rules of its
    /// own, and is not offered here.
    /// </summary>
    private Broker FindBroker(string brokerName, InvestmentScope scope) =>
        _repository.GetBrokerList(scope).FirstOrDefault(broker => broker.Name == brokerName)
            ?? throw new KeyNotFoundException($"Broker \"{brokerName}\" was not found.");

    private static InvestmentScope ParseScope(string? value)
    {
        if (!InvestmentScopeParser.TryParse(value, out var scope))
        {
            throw new ArgumentException($"\"{value}\" is not a recognised investment scope.", nameof(value));
        }

        return scope;
    }

    private static string Required(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{parameterName} is required.", parameterName)
            : value;

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(AssetMoveService), operationName, EntityType);
    }
}
