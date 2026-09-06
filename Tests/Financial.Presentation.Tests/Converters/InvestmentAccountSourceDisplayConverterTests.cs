using System.Globalization;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class InvestmentAccountSourceDisplayConverterTests
{
    private readonly InvestmentAccountSourceDisplayConverter _converter = new();

    private static readonly CreditCardDTO PlatinumVisa = new()
    {
        Id = Guid.NewGuid(),
        Name = "Platinum Visa 8003",
        IsActive = true,
        HasReferences = true,
    };

    [Fact]
    public void Convert_SourceNone_ReturnsDash()
    {
        var result = _converter.Convert(["None", null, new[] { PlatinumVisa }], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("—");
    }

    [Fact]
    public void Convert_SourceCreditCard_ReturnsLinkedCardName()
    {
        var result = _converter.Convert(
            ["CreditCard", PlatinumVisa.Id, new[] { PlatinumVisa }], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Platinum Visa 8003");
    }

    [Fact]
    public void Convert_SourceCreditCardWithUnresolvedId_ReturnsDash()
    {
        var result = _converter.Convert(
            ["CreditCard", Guid.NewGuid(), new[] { PlatinumVisa }], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("—");
    }

    [Fact]
    public void Convert_SourceReserveBucketsSum_ReturnsSumLabel()
    {
        var result = _converter.Convert(["ReserveBucketsSum", null, Array.Empty<CreditCardDTO>()], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Sum of reserve buckets");
    }

    [Fact]
    public void Convert_UnexpectedValues_ReturnsDash()
    {
        var result = _converter.Convert([], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("—");
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var act = () => _converter.ConvertBack("—", [typeof(string)], null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
