using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

internal static class AssetMutationHelper
{
    public delegate bool TryParseDelegate<TEnum>(string? value, out TEnum parsed);

    public static async Task<AssetDetailsDTO?> ExecuteParsedMutationAsync<TEnum>(
        IInvestmentRepository repository,
        INavigationService navigationService,
        string? brokerName,
        string? portfolioName,
        string? assetName,
        string? typeValue,
        TryParseDelegate<TEnum> parser,
        Func<Asset, TEnum, bool> mutation)
    {
        if (!parser(typeValue, out var parsed))
        {
            return null;
        }

        return await ExecuteAssetMutationAsync(
            repository,
            navigationService,
            brokerName,
            portfolioName,
            assetName,
            asset => mutation(asset, parsed)).ConfigureAwait(false);
    }

    public static async Task<AssetDetailsDTO?> ExecuteAssetMutationAsync(
        IInvestmentRepository repository,
        INavigationService navigationService,
        string? brokerName,
        string? portfolioName,
        string? assetName,
        Func<Asset, bool> mutation)
    {
        if (AssetContextValidator.IsInvalid(brokerName, portfolioName, assetName))
        {
            return null;
        }

        var asset = repository.GetAsset(brokerName!, portfolioName!, assetName!);
        if (asset == null)
        {
            return null;
        }

        // The mutation runs inside the save, not before it: the whole document is re-serialized
        // on write, and a change applied outside that exclusion can be walked half-applied.
        if (!await repository.ApplyAndSaveAsync(() => mutation(asset)).ConfigureAwait(false))
        {
            return null;
        }

        return navigationService.GetAssetDetails(brokerName!, portfolioName!, assetName!);
    }
}
