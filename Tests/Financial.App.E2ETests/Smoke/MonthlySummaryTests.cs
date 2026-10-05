using Financial.App.E2ETests.Infrastructure;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Collection(E2ECollection.Name)]
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
            window.FindById("nav-monthly").Press();
            window.FindById("monthly-tabs");
            window.SelectTab("Summary");

            window.FindById("monthly-category-total").Name.Should().Be("Category total 0.00");
            window.FindById("monthly-banks-grid").FindByName("Barclays");
        });
    }
}
