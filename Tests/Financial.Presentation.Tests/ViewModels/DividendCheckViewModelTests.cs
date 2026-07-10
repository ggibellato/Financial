using Financial.Application.Configuration;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Financial.Presentation.Tests.ViewModels;

public class DividendCheckViewModelTests
{
    private readonly StubDividendService _dividendService = new();

    [Fact]
    public void Check_WithEmptyTicker_SetsErrorMessageAndDoesNotThrow()
    {
        var vm = BuildViewModel();
        vm.Ticker = string.Empty;

        Action act = () => vm.CheckCommand.Execute(null);

        act.Should().NotThrow();
        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_WhenServiceThrows_SetsErrorMessageInsteadOfCrashing()
    {
        _dividendService.ThrowOnGetSummary = new InvalidOperationException("Ticker not found.");
        var vm = BuildViewModel();
        vm.Ticker = "INVALIDTICKER";

        Action act = () => vm.CheckCommand.Execute(null);

        act.Should().NotThrow();
        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("INVALIDTICKER");
    }

    [Fact]
    public void Check_WhenServiceThrows_ClearsPreviousResults()
    {
        _dividendService.Summary = MakeSummary();
        var vm = BuildViewModel();
        vm.Ticker = "GOOD";
        vm.CheckCommand.Execute(null);
        vm.History.Should().NotBeEmpty();

        _dividendService.ThrowOnGetSummary = new InvalidOperationException("boom");
        vm.Ticker = "BAD";
        vm.CheckCommand.Execute(null);

        vm.History.Should().BeEmpty();
        vm.YearTotals.Should().BeEmpty();
        vm.SummaryName.Should().BeEmpty();
    }

    [Fact]
    public void Check_WithValidTicker_ClearsPreviousError()
    {
        _dividendService.ThrowOnGetSummary = new InvalidOperationException("boom");
        var vm = BuildViewModel();
        vm.Ticker = "BAD";
        vm.CheckCommand.Execute(null);
        vm.HasError.Should().BeTrue();

        _dividendService.ThrowOnGetSummary = null;
        _dividendService.Summary = MakeSummary();
        vm.Ticker = "GOOD";
        vm.CheckCommand.Execute(null);

        vm.HasError.Should().BeFalse();
        vm.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Check_WithValidTicker_PopulatesSummary()
    {
        _dividendService.Summary = MakeSummary();
        var vm = BuildViewModel();
        vm.Ticker = "TICK";

        vm.CheckCommand.Execute(null);

        vm.HasError.Should().BeFalse();
        vm.SummaryName.Should().Contain("TICK");
        vm.History.Should().HaveCount(1);
        vm.YearTotals.Should().HaveCount(1);
    }

    private DividendCheckViewModel BuildViewModel() =>
        new(_dividendService, Options.Create(new DividendOptions { DefaultExchange = "BVMF" }));

    private static DividendSummaryDTO MakeSummary() => new()
    {
        Exchange = "BVMF",
        Ticker = "TICK",
        Name = "Sample Asset",
        CurrentPrice = 10m,
        PriceAsOf = DateTimeOffset.UtcNow,
        AverageDividendLastFiveYears = 1m,
        DividendYieldPercent = 10m,
        PriceMaxBuy = 12m,
        DiscountPercent = 5m,
        History = [new DividendHistoryItemDTO { Type = "Dividend", Date = DateTime.Today, Value = 1m }],
        YearTotals = [new DividendYearTotalDTO { Year = DateTime.Today.Year, Total = 1m }]
    };

    private sealed class StubDividendService : IDividendService
    {
        public DividendSummaryDTO? Summary { get; set; }
        public Exception? ThrowOnGetSummary { get; set; }

        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request) =>
            Summary?.History ?? [];

        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request)
        {
            if (ThrowOnGetSummary is not null)
                throw ThrowOnGetSummary;

            return Summary ?? throw new InvalidOperationException("No summary configured.");
        }
    }
}
