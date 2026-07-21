namespace Financial.Investment.Application.Configuration;

public sealed class DividendOptions
{
    public const string SectionName = "Dividends";
    public string DefaultExchange { get; init; } = "BVMF";
}
