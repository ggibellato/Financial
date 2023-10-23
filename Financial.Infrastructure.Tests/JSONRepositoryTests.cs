using Financial.Common;
using Financial.Model;
using FinancialModel.Infrastructure;
using FluentAssertions;
using System.Text.Json;

namespace Financial.Infrastructure.Tests;

public class JSONRepositoryTests
{
    private readonly JSONRepository _sut = new JSONRepository();

    [Fact]
    public void GetAllAssets_ShouldReturn_Values()
    {
        var result = _sut.GetAllAssets();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Test2()
    {
        var json = "{\r\n\"Date\": \"2023-02-24T00:00:00\",\r\n\"Type\": \"Sell\",\r\n\"Quantity\": 0.0351,\r\n\"UnitPrice\": 56.9778,\r\n\"Fees\": 0.00000000\r\n}";
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };

        var operation = JsonSerializer.Deserialize<Operation>(json, options);
    }

    [Fact]
    public void Test3()
    {
        var json = "{\"Name\":\"Test\",\"Portifolios\": []}";
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };

        var broker = JsonSerializer.Deserialize<Broker>(json, options);
    }
}