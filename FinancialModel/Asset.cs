using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Asset
{
    [JsonInclude]
    public string Name { get; private set; }

    [JsonInclude]
    public string ISIN { get; private set; }

    [JsonInclude]
    public string Exchange{ get; private set; }

    [JsonInclude]
    public string Ticker { get; private set; }

    [JsonInclude]
    public List<Operation> Operations { get; private set; } = new List<Operation>();

    [JsonInclude]
    public List<Credit> Credits { get; private set; } = new List<Credit>();

    [JsonConstructor]
    private Asset() {}

    private Asset(string name, string isin, string exchange, string ticker) : this()
    {
        Name = name;
        ISIN = isin;
        Exchange = exchange;
        Ticker = ticker;
    }

    public static Asset Create(string name, string isin, string exchange, string ticker) => new(name, isin, exchange, ticker);
}
