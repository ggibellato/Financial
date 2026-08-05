using Financial.Presentation.App.Helpers;
using FluentAssertions;

namespace Financial.Presentation.Tests.Helpers;

public class DateFormatHelperTests
{
    [Fact]
    public void DisplayDatePattern_IsUkDayMonthYearFormat()
    {
        DateFormatHelper.DisplayDatePattern.Should().Be("dd/MM/yyyy");
    }
}
