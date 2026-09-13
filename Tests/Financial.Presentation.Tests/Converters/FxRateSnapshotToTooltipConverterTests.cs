using System.Globalization;
using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class FxRateSnapshotToTooltipConverterTests
{
    private readonly FxRateSnapshotToTooltipConverter _converter = new();

    [Fact]
    public void Convert_CurrencyAndSnapshot_ReturnsThreeLineText()
    {
        var snapshot = new FxRateSnapshotDTO
        {
            ToCurrency = "GBP",
            Rate = 5.123456m,
            Source = "Frankfurter",
            RetrievedAt = new DateTimeOffset(2026, 9, 13, 14, 30, 0, TimeSpan.Zero)
        };

        var result = _converter.Convert(["BRL", snapshot], typeof(string), null, CultureInfo.InvariantCulture);

        var expectedRetrieved = snapshot.RetrievedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        result.Should().Be($"1 BRL = 5.123456 GBP\nSource: Frankfurter\nRetrieved: {expectedRetrieved}");
    }

    [Fact]
    public void Convert_NullSnapshot_ReturnsEmpty()
    {
        var result = _converter.Convert(["BRL", null], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_MissingCurrency_ReturnsEmpty()
    {
        var snapshot = new FxRateSnapshotDTO { ToCurrency = "GBP", Rate = 5m, Source = "Frankfurter", RetrievedAt = DateTimeOffset.UtcNow };

        var result = _converter.Convert([null, snapshot], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var act = () => _converter.ConvertBack("value", [typeof(string), typeof(FxRateSnapshotDTO)], null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
