namespace Financial.Application.Configuration;

public sealed class AssetPriceFetchOptions
{
    public const string SectionName = "AssetPriceFetch";
    public List<AssetPriceFetch> Portfolios { get; set; } = [];
}
