using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using Financial.Presentation.Tests.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TaxWorkbookViewModelTests
{
    private static readonly TaxWorkbookOptionDTO[] Options =
    [
        new() { Jurisdiction = Jurisdiction.BR, TaxYear = "2025" },
        new() { Jurisdiction = Jurisdiction.BR, TaxYear = "2026" },
        new() { Jurisdiction = Jurisdiction.UK, TaxYear = "2025/26" },
    ];

    private static TaxWorkbookEntryDTO Entry(EventCategory category = EventCategory.Dividend) => new()
    {
        Id = Guid.NewGuid(),
        Date = new DateTime(2026, 6, 1),
        EventCategory = category,
        GrossAmount = 100m,
        WithheldAmount = 10m,
        NetAmount = 90m,
        CalculationStatus = CalculationStatus.Final,
        EvidenceReference = Guid.NewGuid(),
    };

    private static (TaxWorkbookViewModel ViewModel, StubTaxWorkbookService Service, StubDialogService Dialog) CreateViewModel(
        StubTaxWorkbookService? service = null)
    {
        service ??= new StubTaxWorkbookService();
        var dialog = new StubDialogService();
        var viewModel = new TaxWorkbookViewModel(service, dialog, new RecordingLogger<TaxWorkbookViewModel>());
        return (viewModel, service, dialog);
    }

    [Fact]
    public async Task RefreshOptionsAsync_DefaultsSelectionToFirstOptionAndLoadsItsWorkbook()
    {
        var service = new StubTaxWorkbookService
        {
            Options = [.. Options],
            WorkbookFactory = (j, y) => new TaxWorkbookDTO
            {
                Jurisdiction = Enum.Parse<Jurisdiction>(j), TaxYear = y, Entries = [Entry()], CategoryTotals = [], CalculationStatus = CalculationStatus.Final,
            },
        };
        var (vm, _, _) = CreateViewModel(service);

        await vm.RefreshOptionsAsync();
        await vm.RefreshWorkbookAsync();

        vm.SelectedJurisdiction.Should().Be("BR");
        vm.SelectedTaxYear.Should().Be("2025");
        vm.Jurisdictions.Should().BeEquivalentTo(["BR", "UK"]);
        vm.TaxYearsForJurisdiction.Should().BeEquivalentTo(["2025", "2026"]);
        vm.Entries.Should().HaveCount(1);
        service.LastGetWorkbookRequest.Should().Be(("BR", "2025"));
    }

    [Fact]
    public async Task SelectingAJurisdiction_ResetsTaxYearToItsFirstOptionAndRefetches()
    {
        var service = new StubTaxWorkbookService { Options = [.. Options] };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();

        vm.SelectedJurisdiction = "UK";
        await vm.RefreshWorkbookAsync();

        vm.SelectedTaxYear.Should().Be("2025/26");
        vm.TaxYearsForJurisdiction.Should().BeEquivalentTo(["2025/26"]);
        service.LastGetWorkbookRequest.Should().Be(("UK", "2025/26"));
    }

    [Fact]
    public async Task SelectingATaxYear_KeepsJurisdictionAndRefetches()
    {
        var service = new StubTaxWorkbookService { Options = [.. Options] };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();

        vm.SelectedTaxYear = "2026";
        await vm.RefreshWorkbookAsync();

        vm.SelectedJurisdiction.Should().Be("BR");
        service.LastGetWorkbookRequest.Should().Be(("BR", "2026"));
    }

    [Fact]
    public async Task OptionsLoadFailure_SurfacesOptionsErrorAndRetryReloads()
    {
        var service = new StubTaxWorkbookService { ThrowOnGetWorkbookOptions = new InvalidOperationException("boom") };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();

        vm.OptionsError.Should().Be("boom");

        service.ThrowOnGetWorkbookOptions = null;
        service.Options = [.. Options];
        await vm.RefreshOptionsAsync();

        vm.OptionsError.Should().BeNull();
        vm.Jurisdictions.Should().BeEquivalentTo(["BR", "UK"]);
    }

    [Fact]
    public async Task WorkbookLoadFailure_SurfacesWorkbookErrorAndRetryReloads()
    {
        var service = new StubTaxWorkbookService { Options = [.. Options], ThrowOnGetWorkbook = new InvalidOperationException("workbook failed") };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();
        await vm.RefreshWorkbookAsync();

        vm.WorkbookError.Should().Be("workbook failed");

        service.ThrowOnGetWorkbook = null;
        await vm.RefreshWorkbookAsync();

        vm.WorkbookError.Should().BeNull();
    }

    [Fact]
    public async Task NoOptionsAtAll_NeverAttemptsAWorkbookFetch()
    {
        var service = new StubTaxWorkbookService { Options = [] };
        var (vm, _, _) = CreateViewModel(service);

        await vm.RefreshOptionsAsync();

        vm.Jurisdictions.Should().BeEmpty();
        vm.SelectedJurisdiction.Should().BeNull();
        service.LastGetWorkbookRequest.Should().BeNull();
    }

    [Fact]
    public async Task CanExportCsv_FalseForAnEmptyWorkbook()
    {
        var service = new StubTaxWorkbookService
        {
            Options = [.. Options],
            WorkbookFactory = (j, y) => new TaxWorkbookDTO { Jurisdiction = Enum.Parse<Jurisdiction>(j), TaxYear = y, Entries = [], CategoryTotals = [], CalculationStatus = null },
        };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();
        await vm.RefreshWorkbookAsync();

        vm.CanExportCsv.Should().BeFalse();
    }

    [Fact]
    public async Task CanExportCsv_TrueOnceEntriesExist()
    {
        var service = new StubTaxWorkbookService
        {
            Options = [.. Options],
            WorkbookFactory = (j, y) => new TaxWorkbookDTO { Jurisdiction = Enum.Parse<Jurisdiction>(j), TaxYear = y, Entries = [Entry()], CategoryTotals = [], CalculationStatus = CalculationStatus.Final },
        };
        var (vm, _, _) = CreateViewModel(service);
        await vm.RefreshOptionsAsync();
        await vm.RefreshWorkbookAsync();

        vm.CanExportCsv.Should().BeTrue();
    }

    [Fact]
    public async Task ExportCsvCommand_PromptsForASaveLocation_UsingTheExpectedFilename()
    {
        var service = new StubTaxWorkbookService
        {
            Options = [.. Options],
            WorkbookFactory = (j, y) => new TaxWorkbookDTO { Jurisdiction = Enum.Parse<Jurisdiction>(j), TaxYear = y, Entries = [Entry()], CategoryTotals = [], CalculationStatus = CalculationStatus.Final },
        };
        var (vm, _, dialog) = CreateViewModel(service);
        dialog.SaveFileDialogResult = null;
        await vm.RefreshOptionsAsync();
        await vm.RefreshWorkbookAsync();

        vm.ExportCsvCommand.Execute(null);

        dialog.LastSaveFileDialogRequest!.Value.SuggestedFileName.Should().Be("tax-workbook-BR-2025.csv");
    }

    [Fact]
    public void BuildCsv_ProducesExactly13ColumnsInOrder()
    {
        var workbook = new TaxWorkbookDTO { Jurisdiction = Jurisdiction.BR, TaxYear = "2026", Entries = [], CategoryTotals = [], CalculationStatus = null };

        var csv = TaxWorkbookViewModel.BuildCsv(workbook);

        csv.Split("\r\n")[0].Split(',').Should().Equal(
            "Date", "Jurisdiction", "TaxYear", "EventCategory", "Proceeds", "CostBasis", "GainLoss",
            "GrossAmount", "WithheldAmount", "NetAmount", "Currency", "CalculationStatus", "EvidenceReference");
    }

    [Fact]
    public void BuildCsv_OneRowPerEntry_WithCurrencyDerivedFromJurisdiction()
    {
        var entry = Entry();
        var workbook = new TaxWorkbookDTO { Jurisdiction = Jurisdiction.UK, TaxYear = "2025/26", Entries = [entry], CategoryTotals = [], CalculationStatus = CalculationStatus.Final };

        var csv = TaxWorkbookViewModel.BuildCsv(workbook);
        var rows = csv.Split("\r\n").Skip(1).ToList();

        rows.Should().HaveCount(1);
        rows[0].Should().Contain(",UK,2025/26,Dividend,");
        rows[0].Should().Contain(",GBP,Final,");
        rows[0].Should().Contain(entry.EvidenceReference.ToString());
    }
}
