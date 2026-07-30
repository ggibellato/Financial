using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DoubleToDataGridLengthConverterTests
{
    private readonly DoubleToDataGridLengthConverter _converter = new();

    [Fact]
    public void Convert_DoubleValue_ReturnsPixelDataGridLength()
    {
        var result = _converter.Convert(150.0, typeof(DataGridLength), null, CultureInfo.InvariantCulture);

        result.Should().Be(new DataGridLength(150.0));
    }

    [Fact]
    public void Convert_NonDoubleValue_ReturnsAuto()
    {
        var result = _converter.Convert("not a double", typeof(DataGridLength), null, CultureInfo.InvariantCulture);

        result.Should().Be(DataGridLength.Auto);
    }

    [Fact]
    public void ConvertBack_DataGridLengthValue_ReturnsPixelWidth()
    {
        var result = _converter.ConvertBack(new DataGridLength(200.0), typeof(double), null, CultureInfo.InvariantCulture);

        result.Should().Be(200.0);
    }

    [Fact]
    public void ConvertBack_NonDataGridLengthValue_ReturnsBindingDoNothing()
    {
        var result = _converter.ConvertBack("not a length", typeof(double), null, CultureInfo.InvariantCulture);

        result.Should().Be(Binding.DoNothing);
    }
}
