> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Serialization (`*Serializer.cs`, `*Adapter.cs` in `Financial.Infrastructure/`)

## What to test

- **Round-trip**: `Deserialize(Serialize(x))` returns a structurally equivalent object graph
- **Object graph preservation**: nested collections (Brokers → Portfolios → Assets → Transactions/Credits) survive the round-trip
- **Field correctness**: specific field values (name, currency, type, quantity) are preserved, not just that the result is non-null
- **Non-empty output**: serialized output is a non-empty string

## Layer assignment

**Unit** — serialization is pure transformation: domain graph → JSON string → domain graph. No file I/O, no async, no external services in the serializer itself.

Note: `InvestmentsLoader.LoadSync` reads from an `IStorage` implementation. That integration is tested in Infrastructure service tests. Serializer tests focus on the serialize/deserialize logic alone.

## Setup pattern

```csharp
[Fact]
public void SerializeDeserialize_RoundTripPreservesStructure()
{
    // Build a representative object graph using domain factory methods
    var investments = Investments.Create();
    var broker = Broker.Create("Broker A", "USD");
    var portfolio = broker.AddPortfolio("Default");
    portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
    investments.AddBroker(broker);

    // Serialize then deserialize
    var json = InvestmentsJsonSerializer.Serialize(investments);
    var result = InvestmentsJsonSerializer.Deserialize(json);

    // Assert structure — use .Which to drill into single-element collections
    result.Should().NotBeNull();
    var brokerResult = result.Brokers.Should().ContainSingle().Which;
    brokerResult.Name.Should().Be("Broker A");
    var portfolioResult = brokerResult.Portfolios.Should().ContainSingle().Which;
    portfolioResult.Assets.Should().ContainSingle()
        .Which.Name.Should().Be("Asset A");
}

[Fact]
public void Serialize_ProducesNonEmptyJson()
{
    var investments = Investments.Create();
    investments.AddBroker(Broker.Create("Broker A", "USD"));

    var json = InvestmentsJsonSerializer.Serialize(investments);

    json.Should().NotBeNullOrWhiteSpace();
}

// Round-trip with transactions and credits
[Fact]
public void SerializeDeserialize_PreservesTransactionsAndCredits()
{
    var investments = Investments.Create();
    var broker = Broker.Create("Broker A", "USD");
    var portfolio = broker.AddPortfolio("Default");
    var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
    asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1),
        Transaction.TransactionType.Buy, 10m, 5m, 0m));
    portfolio.AddAsset(asset);
    investments.AddBroker(broker);

    var json = InvestmentsJsonSerializer.Serialize(investments);
    var result = InvestmentsJsonSerializer.Deserialize(json);

    result.Brokers.Single().Portfolios.Single().Assets.Single()
        .Transactions.Should().ContainSingle()
        .Which.Quantity.Should().Be(10m);
}
```

## When to skip

- JSON property name mapping with no custom logic (System.Text.Json handles this)
- Adapter classes that only delegate to the underlying serializer without transformation

## Examples from project

| Instance | Test focus |
|---|---|
| `InvestmentsJsonSerializer` | Full hierarchy round-trip: Investments → Broker → Portfolio → Asset → Transactions + Credits |
