namespace Financial.Infrastructure.Interfaces;

public class FinanceQuoteRequest
{
    public string? Ticker { get; set; }
    public string? Exchange { get; set; }
    public string? Currency { get; set; }
    public string? Name { get; set; }
}
