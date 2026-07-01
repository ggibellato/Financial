namespace Financial.Api.Options;

public sealed class AssetPriceFetchOptions
{
    public const string SectionName = "AssetPriceFetch";
    public List<PortfolioReference> Portfolios { get; set; } = [];
}
