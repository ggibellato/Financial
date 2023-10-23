using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Asset
{
    [JsonInclude]
    public string Name { get; private set; }

    [JsonInclude]
    public List<Operation> Operations { get; private set; } = new List<Operation>();

    [JsonInclude]
    public List<Credit> Credits { get; private set; } = new List<Credit>();

    [JsonConstructor]
    private Asset() {}

    private Asset(string name) : this()
    {
        Name = name;
    }

    public static Asset Create(string name) => new(name);
}
