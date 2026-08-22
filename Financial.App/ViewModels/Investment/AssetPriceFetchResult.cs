namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// One row of a batch price-fetch run. Deliberately separate from <c>AssetPriceDTO</c> (the wire
/// type) so a failed fetch can still produce a row - the DTO has no field for an error, and adding
/// one would be an API-contract change for a WPF-only display concern.
/// </summary>
public sealed class AssetPriceFetchResult
{
    public required string Ticker { get; init; }
    public required string Name { get; init; }
    public decimal? Price { get; init; }
    public string? Error { get; init; }

    public bool HasError => Error != null;

    public static AssetPriceFetchResult Success(string ticker, string name, decimal price) =>
        new() { Ticker = ticker, Name = name, Price = price };

    public static AssetPriceFetchResult Failure(string ticker, string name, string error) =>
        new() { Ticker = ticker, Name = name, Error = error };
}
