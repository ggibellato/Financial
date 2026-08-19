using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class ControleMaeViewModelTests
{
    private static (ControleMaeViewModel ViewModel, StubControleMaeService Service) CreateViewModel(bool confirm = true) =>
        CreateViewModel(_ => confirm);

    private static (ControleMaeViewModel ViewModel, StubControleMaeService Service) CreateViewModel(Func<string, bool> confirm, RecordingLogger<ControleMaeViewModel>? logger = null)
    {
        var service = new StubControleMaeService();
        var viewModel = new ControleMaeViewModel(service, confirm, logger ?? new RecordingLogger<ControleMaeViewModel>());
        return (viewModel, service);
    }

    private static MaeLedgerEntryDTO CreateEntry(DateOnly date, string description = "Rent") => new()
    {
        Id = Guid.NewGuid(), Date = date, Description = description, Note = string.Empty,
        SourceCurrency = "BRL", BrlValue = 100m, GbpValue = 20m,
    };

    [Fact]
    public async Task RefreshEntriesAsync_LoadsEntriesFromDate()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateOnly.FromDateTime(DateTime.Today);
        service.Entries = [CreateEntry(today), CreateEntry(today.AddDays(-400))];
        viewModel.FromDate = today.AddDays(-1).ToDateTime(TimeOnly.MinValue);

        await viewModel.RefreshEntriesAsync();

        service.LastFromDate.Should().Be(today.AddDays(-1));
        viewModel.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task SettingFromDate_RefetchesEntriesButNotTotals()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.RefreshEntriesAsync();
        await viewModel.RefreshTotalsAsync();
        var totalsCallsBefore = service.GetTotalsCallCount;
        var entriesCallsBefore = service.GetEntriesFromDateCallCount;

        viewModel.FromDate = DateTime.Today.AddDays(-30);
        await viewModel.RefreshEntriesAsync();

        service.GetEntriesFromDateCallCount.Should().BeGreaterThan(entriesCallsBefore);
        service.GetTotalsCallCount.Should().Be(totalsCallsBefore);
    }

    [Fact]
    public async Task CreateEntry_ValidFormBrl_CallsServiceAndClosesForm()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowCreateFormCommand.Execute(null);
        viewModel.CreateDate = DateTime.Today;
        viewModel.CreateDescription = "Salary";
        viewModel.CreateCurrency = "BRL";
        viewModel.CreateValue = "500";

        await viewModel.SubmitCreateAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.SourceCurrency.Should().Be("BRL");
        service.LastCreateRequest.SourceValue.Should().Be(500m);
        viewModel.IsCreateFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CreateEntry_ValidFormGbp_CallsServiceWithGbpCurrency()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowCreateFormCommand.Execute(null);
        viewModel.CreateDate = DateTime.Today;
        viewModel.CreateDescription = "Salary";
        viewModel.CreateCurrency = "GBP";
        viewModel.CreateValue = "100";

        await viewModel.SubmitCreateAsync();

        service.LastCreateRequest!.SourceCurrency.Should().Be("GBP");
    }

    [Theory]
    [InlineData(null, "Salary", "100")]
    [InlineData("2026-01-01", "", "100")]
    [InlineData("2026-01-01", "Salary", "0")]
    public async Task CreateEntry_InvalidForm_BlocksSaveWithoutServiceCall(string? date, string description, string value)
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowCreateFormCommand.Execute(null);
        viewModel.CreateDate = date is null ? null : DateTime.Parse(date);
        viewModel.CreateDescription = description;
        viewModel.CreateValue = value;

        await viewModel.SubmitCreateAsync();

        service.LastCreateRequest.Should().BeNull();
        viewModel.CreateSaveError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EditEntry_ValidFormBothValues_CallsUpdateServiceWithParsedValues()
    {
        var (viewModel, service) = CreateViewModel();
        var entry = CreateEntry(DateOnly.FromDateTime(DateTime.Today));
        service.Entries = [entry];
        await viewModel.RefreshEntriesAsync();

        viewModel.EditEntryCommand.Execute(entry);
        viewModel.EditBrlValue = "150";
        viewModel.EditGbpValue = "30";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Value.Id.Should().Be(entry.Id);
        service.LastUpdateRequest.Value.Request.BrlValue.Should().Be(150m);
        service.LastUpdateRequest.Value.Request.GbpValue.Should().Be(30m);
        viewModel.IsEditFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task EditEntry_BlankField_MapsToNull()
    {
        var (viewModel, service) = CreateViewModel();
        var entry = CreateEntry(DateOnly.FromDateTime(DateTime.Today));
        viewModel.EditEntryCommand.Execute(entry);
        viewModel.EditBrlValue = "150";
        viewModel.EditGbpValue = "";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest!.Value.Request.GbpValue.Should().BeNull();
    }

    [Fact]
    public async Task EditEntry_InvalidForm_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, service) = CreateViewModel();
        var entry = CreateEntry(DateOnly.FromDateTime(DateTime.Today));
        viewModel.EditEntryCommand.Execute(entry);
        viewModel.EditBrlValue = "not-a-number";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditSaveError.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteEntry_ConfirmedAndDeclined_CallsOrSkipsService(bool confirmed)
    {
        var (viewModel, service) = CreateViewModel(confirmed);
        var entry = CreateEntry(DateOnly.FromDateTime(DateTime.Today));

        await viewModel.DeleteEntryAsync(entry);

        if (confirmed)
        {
            service.LastDeletedId.Should().Be(entry.Id);
        }
        else
        {
            service.LastDeletedId.Should().BeNull();
        }
    }

    [Fact]
    public async Task RefreshTotalsAsync_ServiceFails_LogsAWarningAndKeepsLastKnownTotals()
    {
        var logger = new RecordingLogger<ControleMaeViewModel>();
        var (viewModel, service) = CreateViewModel(_ => true, logger);
        service.ThrowOnGetTotals = new InvalidOperationException("total BRL 9999.99");

        await viewModel.RefreshTotalsAsync();

        // The constructor's background totals refresh may also hit the failing service, so more
        // than one identical warning can be recorded - assert on all of them.
        var warnings = logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ToList();
        warnings.Should().NotBeEmpty();
        warnings.Should().AllSatisfy(w =>
        {
            w.Message.Should().Contain(nameof(InvalidOperationException));
            w.Message.Should().NotContain("9999.99", "exception messages may embed ledger values and must stay out of the log");
        });
    }
}
