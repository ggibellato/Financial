using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CorporateActionFormViewModelTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    [Fact]
    public void CreateForAdd_DefaultsEffectiveDateToTodayWithBlankRatioAndNote()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);

        vm.Mode.Should().Be(CorporateActionFormMode.Add);
        vm.EffectiveDate.Should().Be(DateTime.Today);
        vm.Type.Should().Be("Split");
        vm.RatioNumerator.Should().Be(0m);
        vm.RatioDenominator.Should().Be(0m);
        vm.Note.Should().BeEmpty();
        vm.Title.Should().Be("New corporate action");
        vm.ConfirmLabel.Should().Be("Add corporate action");
    }

    [Fact]
    public void CreateForUpdate_SetsTitleAndConfirmLabelForUpdateMode()
    {
        var id = Guid.NewGuid();
        var vm = CorporateActionFormViewModel.CreateForUpdate(
            BrokerName, PortfolioName, AssetName, id, new DateTime(2026, 1, 1), "Split", 3m, 1m, "note");

        vm.Mode.Should().Be(CorporateActionFormMode.Update);
        vm.CorporateActionId.Should().Be(id);
        vm.Title.Should().Be("Edit corporate action");
        vm.ConfirmLabel.Should().Be("Save");
        vm.RatioNumerator.Should().Be(3m);
        vm.RatioDenominator.Should().Be(1m);
        vm.Note.Should().Be("note");
    }

    [Fact]
    public void CreateForDelete_SetsTitleAndConfirmLabelForDeleteModeAndIsReadOnly()
    {
        var id = Guid.NewGuid();
        var vm = CorporateActionFormViewModel.CreateForDelete(
            BrokerName, PortfolioName, AssetName, id, new DateTime(2026, 1, 1), "Split", 3m, 1m, "note");

        vm.Mode.Should().Be(CorporateActionFormMode.Delete);
        vm.Title.Should().Be("Delete Corporate Action");
        vm.ConfirmLabel.Should().Be("Delete");
        vm.IsReadOnly.Should().BeTrue();
        vm.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_OneForOneRatio_ShowsExactMessageAndCannotExecute()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.RatioNumerator = 1m;
        vm.RatioDenominator = 1m;

        vm.ValidationMessage.Should().Be("Enter a valid split ratio other than 1-for-1");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_InvalidRatio_ShowsExactMessageAndCannotExecute()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.RatioNumerator = 0m;
        vm.RatioDenominator = 0m;

        vm.ValidationMessage.Should().Be("Enter a valid split ratio other than 1-for-1");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_ValidRatio_CanExecuteAndRaisesCloseRequestedTrue()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.RatioNumerator = 3m;
        vm.RatioDenominator = 1m;

        bool? closedWith = null;
        vm.CloseRequested += (_, result) => closedWith = result;

        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
        vm.ConfirmCommand.Execute(null);

        closedWith.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);

        bool? closedWith = null;
        vm.CloseRequested += (_, result) => closedWith = result;

        vm.CancelCommand.Execute(null);

        closedWith.Should().BeFalse();
    }

    [Fact]
    public void DeleteMode_ConfirmCommand_AlwaysCanExecuteRegardlessOfFields()
    {
        var vm = CorporateActionFormViewModel.CreateForDelete(
            BrokerName, PortfolioName, AssetName, Guid.NewGuid(), DateTime.MinValue, "Split", 0m, 0m, null);

        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ReportSubmitFailed_SetsValidationMessageAndBlocksResubmit()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.RatioNumerator = 3m;
        vm.RatioDenominator = 1m;
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();

        vm.ReportSubmitFailed("Server rejected the split.");

        vm.ValidationMessage.Should().Be("Server rejected the split.");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
        vm.RatioNumerator.Should().Be(3m);
        vm.RatioDenominator.Should().Be(1m);
    }

    [Fact]
    public void ChangingAFieldAfterReportSubmitFailed_ReEvaluatesValidationMessage()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.RatioNumerator = 3m;
        vm.RatioDenominator = 1m;
        vm.ReportSubmitFailed("Server rejected the split.");

        vm.RatioNumerator = 5m;

        vm.ValidationMessage.Should().BeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }
}
