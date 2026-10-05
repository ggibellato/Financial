using Financial.App.E2ETests.Infrastructure;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
public class MonthlySummaryTests
{
    [Fact]
    public void MonthlySummary_ShowsTotals()
    {
        AppSession.Run(nameof(MonthlySummary_ShowsTotals), session =>
        {
            var window = session.Window;
            window.OpenMonthly("Summary");

            window.FindById("monthly-category-total").Name.Should().Be("Category total 0.00");
            window.FindById("monthly-banks-grid").FindByName("Barclays");
        });
    }
}
