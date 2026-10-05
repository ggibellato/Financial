using System.Text.Json;
using System.Text.Json.Serialization;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Unit")]
public abstract class ReferenceConverterTests<T> where T : class
{
    protected abstract T CreateEntity();

    protected abstract Guid IdOf(T entity);

    protected abstract JsonConverter CreateConverter(Dictionary<Guid, T>? lookup);

    private JsonSerializerOptions CreateOptions(Dictionary<Guid, T>? lookup) => new()
    {
        Converters = { CreateConverter(lookup) }
    };

    [Fact]
    public void Read_KnownId_ResolvesToTheLookupInstance()
    {
        var entity = CreateEntity();
        var options = CreateOptions(new Dictionary<Guid, T> { [IdOf(entity)] = entity });

        var result = JsonSerializer.Deserialize<T>($"\"{IdOf(entity)}\"", options);

        result.Should().BeSameAs(entity);
    }

    [Fact]
    public void Read_IdAbsentFromLookup_ThrowsNamingTheMissingId()
    {
        var missingId = Guid.NewGuid();
        var options = CreateOptions([]);

        var act = () => JsonSerializer.Deserialize<T>($"\"{missingId}\"", options);

        act.Should().Throw<JsonException>().WithMessage($"*{missingId}*");
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var options = CreateOptions([]);

        var result = JsonSerializer.Deserialize<T>("null", options);

        result.Should().BeNull();
    }

    [Fact]
    public void Write_EmitsOnlyTheId()
    {
        var entity = CreateEntity();
        var options = CreateOptions(lookup: null);

        var json = JsonSerializer.Serialize(entity, options);

        json.Should().Be($"\"{IdOf(entity)}\"");
    }
}

public class BankReferenceConverterTests : ReferenceConverterTests<Bank>
{
    protected override Bank CreateEntity() => Bank.Create("Barclays", roundUpEnabled: false);
    protected override Guid IdOf(Bank entity) => entity.Id;
    protected override JsonConverter CreateConverter(Dictionary<Guid, Bank>? lookup) => new BankReferenceConverter(lookup);
}

public class CreditCardReferenceConverterTests : ReferenceConverterTests<CreditCard>
{
    protected override CreditCard CreateEntity() => CreditCard.Create("BarclaysPlatinumVisa8003");
    protected override Guid IdOf(CreditCard entity) => entity.Id;
    protected override JsonConverter CreateConverter(Dictionary<Guid, CreditCard>? lookup) => new CreditCardReferenceConverter(lookup);
}

public class IncomeSourceReferenceConverterTests : ReferenceConverterTests<IncomeSource>
{
    protected override IncomeSource CreateEntity() => IncomeSource.Create("Gleison", IncomeGroup.Salary);
    protected override Guid IdOf(IncomeSource entity) => entity.Id;
    protected override JsonConverter CreateConverter(Dictionary<Guid, IncomeSource>? lookup) => new IncomeSourceReferenceConverter(lookup);
}

public class InvestmentAccountReferenceConverterTests : ReferenceConverterTests<InvestmentAccount>
{
    protected override InvestmentAccount CreateEntity() => InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
    protected override Guid IdOf(InvestmentAccount entity) => entity.Id;
    protected override JsonConverter CreateConverter(Dictionary<Guid, InvestmentAccount>? lookup) => new InvestmentAccountReferenceConverter(lookup);
}

public class ReserveBucketReferenceConverterTests : ReferenceConverterTests<ReserveBucket>
{
    protected override ReserveBucket CreateEntity() => ReserveBucket.Create("Investimento", 33.33m);
    protected override Guid IdOf(ReserveBucket entity) => entity.Id;
    protected override JsonConverter CreateConverter(Dictionary<Guid, ReserveBucket>? lookup) => new ReserveBucketReferenceConverter(lookup);
}
