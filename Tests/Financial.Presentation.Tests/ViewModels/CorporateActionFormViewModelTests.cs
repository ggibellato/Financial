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

    [Fact]
    public void SelectingMergerType_RevealsMergerFieldsStepAndHidesSplitFields()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);

        vm.Type = CorporateActionFormValidation.MergerTypeValue;

        vm.IsMerger.Should().BeTrue();
        vm.IsSplit.Should().BeFalse();
        vm.IsMergerFieldsStep.Should().BeTrue();
        vm.IsMergerConfirmStep.Should().BeFalse();
    }

    [Fact]
    public void SplitType_NeverEntersMergerSteps()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);

        vm.IsMergerFieldsStep.Should().BeFalse();
        vm.IsMergerConfirmStep.Should().BeFalse();
    }

    [Fact]
    public void MergerConfirmCommand_OnFieldsStep_AdvancesToConfirmStepWithoutClosing()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;

        bool? closedWith = null;
        vm.CloseRequested += (_, result) => closedWith = result;

        vm.ConfirmCommand.Execute(null);

        closedWith.Should().BeNull();
        vm.IsMergerFieldsStep.Should().BeFalse();
        vm.IsMergerConfirmStep.Should().BeTrue();
        vm.ConfirmLabel.Should().Be("Confirm & Save");
    }

    [Fact]
    public void MergerConfirmCommand_OnConfirmStep_RaisesCloseRequestedTrue()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;
        vm.ConfirmCommand.Execute(null);

        bool? closedWith = null;
        vm.CloseRequested += (_, result) => closedWith = result;
        vm.ConfirmCommand.Execute(null);

        closedWith.Should().BeTrue();
    }

    [Fact]
    public void MergerBackCommand_ReturnsToFieldsStepPreservingEnteredValues()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;
        vm.CashInLieuAmount = 10m;
        vm.ConfirmCommand.Execute(null);
        vm.IsMergerConfirmStep.Should().BeTrue();

        vm.BackCommand.Execute(null);

        vm.IsMergerFieldsStep.Should().BeTrue();
        vm.IsMergerConfirmStep.Should().BeFalse();
        vm.ConfirmLabel.Should().Be("Continue");
        vm.TargetAssetPicker.AssetName.Should().Be("Company B");
        vm.ExchangeRatio.Should().Be(2m);
        vm.CashInLieuAmount.Should().Be(10m);
    }

    [Fact]
    public void MergerConfirmLabel_ReadsContinueThenConfirmAndSave()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;

        vm.ConfirmLabel.Should().Be("Continue");

        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;
        vm.ConfirmCommand.Execute(null);

        vm.ConfirmLabel.Should().Be("Confirm & Save");
    }

    [Fact]
    public void MergerAddMode_MissingTargetAsset_BlocksAdvancingPastFieldsStep()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.ExchangeRatio = 2m;

        vm.ConfirmCommand.Execute(null);

        vm.IsMergerFieldsStep.Should().BeTrue();
        vm.ValidationMessage.Should().Be("Target asset is required");
    }

    [Fact]
    public void MergerExchangeRatioNotPositive_BlocksAdvancingPastFieldsStep()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 0m;

        vm.ConfirmCommand.Execute(null);

        vm.IsMergerFieldsStep.Should().BeTrue();
        vm.ValidationMessage.Should().Be("Exchange ratio must be greater than zero");
    }

    [Fact]
    public void MergerUpdateMode_DoesNotRequireTargetAssetToAdvance()
    {
        var vm = CorporateActionFormViewModel.CreateForUpdate(
            BrokerName, PortfolioName, AssetName, Guid.NewGuid(), new DateTime(2026, 1, 1), CorporateActionFormValidation.MergerTypeValue, 0m, 0m, null,
            targetAssetName: "Company B", exchangeRatio: 2m);

        vm.ConfirmCommand.Execute(null);

        vm.IsMergerConfirmStep.Should().BeTrue();
    }

    [Fact]
    public void ReportSubmitFailed_OnMergerConfirmStep_KeepsConfirmEnabledForRetry()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.EffectiveDate = new DateTime(2026, 1, 1);
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;
        vm.ConfirmCommand.Execute(null);
        vm.IsMergerConfirmStep.Should().BeTrue();

        vm.ReportSubmitFailed("Server rejected the merger.");

        vm.ValidationMessage.Should().Be("Server rejected the merger.");
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void MergerConfirmSummary_MatchesExpectedWording()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(
            BrokerName, PortfolioName, AssetName, sourceQuantity: 100m, sourceCostBasis: 5000m);
        vm.Type = CorporateActionFormValidation.MergerTypeValue;
        vm.TargetAssetPicker!.AssetName = "Company B";
        vm.ExchangeRatio = 2m;

        vm.ConfirmSummary.Should().Be(
            "Your position in BBAS3 (100.00 units) will close and convert into 200.00 units of Company B, carrying over 5,000.00 of cost basis.");
    }
}
