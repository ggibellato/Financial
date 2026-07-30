using System.Globalization;
using System.Windows.Media;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class TransactionTypeToColorConverterTests
{
    private readonly TransactionTypeToColorConverter _converter = new();

    [Fact]
    public void Convert_Buy_ReturnsGreenBrush()
    {
        _converter.Convert("Buy", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_BuyDifferentCase_ReturnsGreenBrush()
    {
        _converter.Convert("buy", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_Sell_ReturnsRedBrush()
    {
        _converter.Convert("Sell", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Red);
    }

    [Fact]
    public void Convert_UnrecognizedString_ReturnsRedBrush()
    {
        _converter.Convert("NotAType", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Red);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsBlackBrush()
    {
        _converter.Convert(42, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Black);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotImplementedException()
    {
        Action act = () => _converter.ConvertBack(Brushes.Green, typeof(string), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }
}
