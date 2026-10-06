using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

[Trait("Category", "Unit")]
public class IncomeSplitViewModelTests
{
    private static (IncomeSplitViewModel ViewModel, StubReserveService Service) CreateViewModel()
    {
        var service = new StubReserveService();
        var viewModel = new IncomeSplitViewModel(service, closeOtherForms: () => { }, TestClock.At(), refresh: () => Task.CompletedTask);
        return (viewModel, service);
    }

    [Fact]
    public async Task SubmitIncomeSplit_ValidForm_CallsServiceAndShowsResultPanel()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = TestClock.LocalToday;
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

    [Fact]
    public async Task DateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = null;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().BeNull();
        viewModel.DateFieldError.Should().Be(viewModel.SplitSaveError);
        viewModel.AmountFieldError.Should().BeNull();
    }

    [Fact]
    public async Task AmountFieldError_ZeroAmount_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitAmount = "0";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().BeNull();
        viewModel.AmountFieldError.Should().Be(viewModel.SplitSaveError);
    }

    [Fact]
    public async Task DescriptionFieldError_MissingDescription_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "";

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().BeNull();
        viewModel.DescriptionFieldError.Should().Be(viewModel.SplitSaveError);
    }

    [Fact]
    public async Task FieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "";
        await viewModel.SubmitSplitAsync();
        viewModel.DescriptionFieldError.Should().NotBeNull();

        viewModel.SplitDescription = "Salary";
        await viewModel.SubmitSplitAsync();

        viewModel.DescriptionFieldError.Should().BeNull();
    }

    [Fact]
    public async Task ShowSplitForm_AfterSuccessfulSubmit_PersistsDateButNotAmountOrDescription()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        var usedDate = TestClock.LocalToday.AddDays(-2);
        viewModel.SplitDate = usedDate;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";
        await viewModel.SubmitSplitAsync();
        viewModel.DismissSplitResultCommand.Execute(null);

        viewModel.ShowSplitFormCommand.Execute(null);

        viewModel.SplitDate.Should().Be(usedDate);
        viewModel.SplitAmount.Should().BeEmpty();
        viewModel.SplitDescription.Should().BeEmpty();
    }

    [Fact]
    public async Task SplitGeneralSaveError_ServiceFails_ShowsTheMessageAsAGeneralError()
    {
        var (viewModel, service) = CreateViewModel();
        service.ThrowOnPostIncomeSplit = new InvalidOperationException("Storage unavailable");
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = TestClock.LocalToday;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        viewModel.SplitGeneralSaveError.Should().Be("Storage unavailable");
        viewModel.LastSplitResult.Should().BeNull();
    }

    [Fact]
    public async Task SplitGeneralSaveError_FieldValidationFails_IsNotShownAsAGeneralError()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = null;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        viewModel.DateFieldError.Should().NotBeNull();
        viewModel.SplitGeneralSaveError.Should().BeNull();
    }

    [Fact]
    public async Task ShowSplitFormFields_IsTrueOnlyWhileTheFormIsOpenWithoutAResult()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowSplitFormFields.Should().BeFalse();

        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.ShowSplitFormFields.Should().BeTrue();

        viewModel.SplitDate = TestClock.LocalToday;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";
        await viewModel.SubmitSplitAsync();
        viewModel.ShowSplitFormFields.Should().BeFalse();

        viewModel.DismissSplitResultCommand.Execute(null);
        viewModel.CancelSplitFormCommand.Execute(null);
        viewModel.ShowSplitFormFields.Should().BeFalse();
    }
}
