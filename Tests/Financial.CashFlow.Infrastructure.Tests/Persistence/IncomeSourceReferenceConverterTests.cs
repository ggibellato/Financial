using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class IncomeSourceReferenceConverterTests
{
    private static JsonSerializerOptions CreateOptions(Dictionary<Guid, IncomeSource>? lookup) => new()
    {
        Converters = { new IncomeSourceReferenceConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var source = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var lookup = new Dictionary<Guid, IncomeSource> { [source.Id] = source };
        var options = CreateOptions(lookup);

        var result = JsonSerializer.Deserialize<IncomeSource>($"\"{source.Id}\"", options);

        result.Should().BeSameAs(source);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<IncomeSource>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Write_EmitsOnlyTheIncomeSourceId()
    {
        var source = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(source, options);

        json.Should().Be($"\"{source.Id}\"");
    }
}
