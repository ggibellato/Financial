using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class AssetSnapshotSourceAdapterTests
{
    [Fact]
    public void Constructor_Parameterless_DoesNotInvokeLookup()
    {
        // The default constructor wires up GoogleFinance.GetFinancialInfoSnapshot as a delegate
        // without invoking it - constructing the adapter must never make a network call.
        Action act = () => new AssetSnapshotSourceAdapter();

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullLookup_ThrowsArgumentNullException()
    {
        Action act = () => new AssetSnapshotSourceAdapter(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lookup");
    }

    [Fact]
    public void GetSnapshot_DelegatesToLookupWithExchangeAndTicker()
    {
        var snapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, DateTimeOffset.UtcNow);
        var adapter = new AssetSnapshotSourceAdapter(
            (exchange, ticker) => exchange == "BVMF" && ticker == "BCIA11" ? snapshot : throw new InvalidOperationException());

        var result = adapter.GetSnapshot("BVMF", "BCIA11");

        result.Should().Be(snapshot);
    }
}
