namespace Financial.Presentation.App.Options;

public class AssetPriceFetchOptions
{
    public const string SectionName = "AssetPriceFetch";
    public List<PortfolioReference> Portfolios { get; set; } = new();
}
