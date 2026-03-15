using System;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Infrastructure.Repositories;

internal static class AssetServiceHelper
{
    public static bool IsInvalidContext(string? brokerName, string? portfolioName, string? assetName)
    {
        return string.IsNullOrWhiteSpace(brokerName) ||
               string.IsNullOrWhiteSpace(portfolioName) ||
               string.IsNullOrWhiteSpace(assetName);
    }

    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = default;
            return false;
        }

        return Enum.TryParse(value, true, out parsed);
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
