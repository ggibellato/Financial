using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class DividendDataSourceAdapterTests
{
    [Fact]
    public void Constructor_Parameterless_DoesNotInvokeLookup()
    {
        // The default constructor wires up DadosMercadoDividend.GetDividendInfo as a delegate
        // without invoking it - constructing the adapter must never make a network call.
        Action act = () => new DividendDataSourceAdapter();

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullLookup_ThrowsArgumentNullException()
    {
        Action act = () => new DividendDataSourceAdapter(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lookup");
    }

    [Fact]
    public void GetDividends_DelegatesToLookupWithTicker()
    {
        var dividends = new List<DividendValue> { new(DividendType.Dividend, new DateTime(2024, 1, 1), 5m) };
        var adapter = new DividendDataSourceAdapter(
            ticker => ticker == "BCIA11" ? dividends : throw new InvalidOperationException());

        var result = adapter.GetDividends("BVMF", "BCIA11");

        result.Should().BeSameAs(dividends);
    }
}
