> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Serialization (`*Serializer.cs`, `*Adapter.cs`)

Examples: `InvestmentSerializerAdapter` (Investment) and `CashFlowSerializerAdapter` (CashFlow).

## What to test

- **Round-trip**: `Deserialize(Serialize(x))` returns a structurally equivalent object graph
- **Object graph preservation**: nested collections (Brokers → Portfolios → Assets → Transactions/Credits) survive the round-trip
- **Field correctness**: specific field values (name, currency, type, quantity) are preserved, not just that the result is non-null
- Any custom transformation the adapter applies beyond plain property mapping (e.g., enum-to-string, date formatting, legacy-field migration on read)

## Layer assignment

**Unit** — serialization is pure transformation: domain graph → JSON string → domain graph. No file I/O, no async, no external services in the serializer itself (that boundary is covered separately by `artifacts/infrastructure-persistence.md`).

## Setup pattern

```csharp
[Fact]
public void SerializeDeserialize_RoundTripPreservesStructure()
{
    var investments = Investments.Create();
    var broker = Broker.Create("Broker A", "USD");
    var portfolio = broker.AddPortfolio("Default");
    portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
    investments.AddBroker(broker);

    var json = InvestmentsJsonSerializer.Serialize(investments);
    var result = InvestmentsJsonSerializer.Deserialize(json);

    result.Should().NotBeNull();
    var brokerResult = result.Brokers.Should().ContainSingle().Which;
    brokerResult.Name.Should().Be("Broker A");
    brokerResult.Portfolios.Should().ContainSingle()
        .Which.Assets.Should().ContainSingle()
        .Which.Name.Should().Be("Asset A");
}
```

## When to skip

- Plain property mapping with no custom logic — trust `System.Text.Json`'s own guarantees; only test the parts of the adapter that do something beyond default (de)serialization
- Note: Application-layer DTOs in this project carry `System.Text.Json` attributes directly (an accepted, documented trade-off given the blast radius vs. benefit for a personal project). Don't add tests trying to prove DTOs are "clean" of serialization concerns.

## Examples from project

| Instance | Test focus |
|---|---|
| `InvestmentsJsonSerializer` | Full hierarchy round-trip: Investments → Broker → Portfolio → Asset → Transactions + Credits |
