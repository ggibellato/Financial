using System;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Infrastructure.Services;

internal static class AssetServiceHelper
{
    public delegate bool TryParseDelegate<TEnum>(string? value, out TEnum parsed);

    public static bool IsInvalidContext(string? brokerName, string? portfolioName, string? assetName)
    {
        return string.IsNullOrWhiteSpace(brokerName) ||
               string.IsNullOrWhiteSpace(portfolioName) ||
               string.IsNullOrWhiteSpace(assetName);
    }

    public static AssetDetailsDTO? ExecuteParsedMutation<TEnum>(
        IRepository repository,
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

        return ExecuteAssetMutation(
            repository,
            navigationService,
            brokerName,
            portfolioName,
            assetName,
            asset => mutation(asset, parsed));
    }

    public static AssetDetailsDTO? ExecuteAssetMutation(
        IRepository repository,
        INavigationService navigationService,
        string? brokerName,
        string? portfolioName,
        string? assetName,
        Func<Asset, bool> mutation)
    {
        if (IsInvalidContext(brokerName, portfolioName, assetName))
        {
            return null;
        }

        var asset = repository.GetAsset(brokerName!, portfolioName!, assetName!);
        if (asset == null)
        {
            return null;
        }

        if (!mutation(asset))
        {
            return null;
        }

        repository.SaveChanges();
        return navigationService.GetAssetDetails(brokerName!, portfolioName!, assetName!);
    }
}

