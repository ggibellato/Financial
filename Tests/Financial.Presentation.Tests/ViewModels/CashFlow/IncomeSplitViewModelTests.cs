using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeSplitViewModelTests
{
    private static (IncomeSplitViewModel ViewModel, StubReserveService Service) CreateViewModel()
    {
        var service = new StubReserveService();
        var viewModel = new IncomeSplitViewModel(service, closeOtherForms: () => { }, refresh: () => Task.CompletedTask);
        return (viewModel, service);
    }

    [Fact]
    public async Task SubmitIncomeSplit_ValidForm_CallsServiceAndShowsResultPanel()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = DateTime.Today;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().NotBeNull();
        service.LastSplitRequest!.Amount.Should().Be(100m);
        service.LastSplitRequest.Description.Should().Be("Salary");
        viewModel.LastSplitResult.Should().Be(service.SplitResult);
        viewModel.HasSplitResult.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "100", "Salary")]
    [InlineData("2026-01-01", "0", "Salary")]
    [InlineData("2026-01-01", "100", "")]
    public async Task SubmitIncomeSplit_InvalidForm_BlocksSaveWithoutServiceCall(string? date, string amount, string description)
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = date is null ? null : DateTime.Parse(date);
        viewModel.SplitAmount = amount;
        viewModel.SplitDescription = description;

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().BeNull();
        viewModel.SplitSaveError.Should().NotBeNullOrEmpty();
    }
}
