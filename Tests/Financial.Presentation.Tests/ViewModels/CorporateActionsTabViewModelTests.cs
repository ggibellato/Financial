using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class CorporateActionsTabViewModelTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    private static (CorporateActionsTabViewModel ViewModel, StubCorporateActionService Service, Spy Spy) Build(
        bool hasContext = true,
        ICorporateActionService? service = null,
        IAssetAdminService? assetAdminService = null)
    {
        var stubService = service as StubCorporateActionService ?? new StubCorporateActionService();
        var spy = new Spy();
        var viewModel = new CorporateActionsTabViewModel(
            stubService,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage,
            assetAdminService);
        return (viewModel, stubService, spy);
    }

    private static CorporateActionFormData ValidFormData(Guid? id = null) => new(
        CorporateActionId: id ?? Guid.Empty,
        EffectiveDate: new DateTime(2026, 1, 1),
        Type: "Split",
        RatioNumerator: 3m,
        RatioDenominator: 1m,
        Note: "note");

    private static CorporateActionFormData MergerFormData(Guid? id = null, string targetAssetName = "Company B", bool createTargetAssetInline = true) => new(
        CorporateActionId: id ?? Guid.Empty,
        EffectiveDate: new DateTime(2026, 1, 1),
        Type: "Merger",
        RatioNumerator: 0m,
        RatioDenominator: 0m,
        Note: "merger note",
        TargetAssetName: targetAssetName,
        CreateTargetAssetInline: createTargetAssetInline,
        ExchangeRatio: 2m,
        CashInLieuAmount: 5m);

    private static Task<CorporateActionFormData?> AsForm(CorporateActionFormData? data) => Task.FromResult(data);

    [Fact]
    public void Load_RebuildsGridFromCorporateActions()
    {
        var (viewModel, _, _) = Build();
        var records = new List<CorporateActionDTO>
        {
            new() { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = new DateTime(2026, 1, 1), RatioFactor = 3m },
            new() { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = new DateTime(2025, 6, 1), RatioFactor = 2m }
        };

        viewModel.Load("ctx", records, AssetName);

        viewModel.CorporateActions.Should().HaveCount(2);
        viewModel.CorporateActions.Select(row => row.Id).Should().BeEquivalentTo(records.Select(r => r.Id));
        viewModel.HasVisibleCorporateActions.Should().BeTrue();
        viewModel.HasNoCorporateActions.Should().BeFalse();
        viewModel.SelectedCorporateAction.Should().BeNull();
    }

    [Fact]
    public void Load_NoCorporateActions_SetsEmptyStateFlags()
    {
        var (viewModel, _, _) = Build();

        viewModel.Load("ctx", [], AssetName);

        viewModel.CorporateActions.Should().BeEmpty();
        viewModel.HasVisibleCorporateActions.Should().BeFalse();
        viewModel.HasNoCorporateActions.Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsCollectionAndFlags()
    {
        var (viewModel, _, _) = Build();
        var record = new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m };
        viewModel.Load("ctx", [record], AssetName);

        viewModel.Clear();

        viewModel.CorporateActions.Should().BeEmpty();
        viewModel.HasNoCorporateActions.Should().BeTrue();
        viewModel.SelectedCorporateAction.Should().BeNull();
    }

    [Fact]
    public void ShowAddCorporateActionFormAsync_DefaultsEffectiveDateToTodayWithBlankRatio()
    {
        var (viewModel, _, _) = Build();

        viewModel.ShowAddCorporateActionFormAsync();
        var formVm = viewModel.FormViewModel!;

        formVm.EffectiveDate.Should().Be(DateTime.Today);
        formVm.RatioNumerator.Should().Be(0m);
        formVm.RatioDenominator.Should().Be(0m);
        formVm.Note.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_NoContext_ShowsInfoAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build(hasContext: false);

        await viewModel.Add(() => AsForm(ValidFormData()));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Information);
    }

    [Fact]
    public async Task Add_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Add(() => AsForm(null));

        service.AddCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCorporateActionService { AddSplitResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(ValidFormData()));

        service.LastAddSplitRequest.Should().NotBeNull();
        service.LastAddSplitRequest!.BrokerName.Should().Be(BrokerName);
        service.LastAddSplitRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastAddSplitRequest.AssetName.Should().Be(AssetName);
        service.LastAddSplitRequest.EffectiveDate.Should().Be(new DateTime(2026, 1, 1));
        service.LastAddSplitRequest.RatioFactor.Should().Be(3m);
        service.LastAddSplitRequest.Note.Should().Be("note");
        spy.AppliedDetails.Should().Be(expectedDetails);
        viewModel.IsFormOpen.Should().BeFalse();
        viewModel.FormViewModel.Should().BeNull();
    }

    [Fact]
    public async Task Add_ServiceThrowsInvestmentRuleViolation_ShowsTheDomainMessageAndDoesNotCrashOrApplyDetails()
    {
        var service = new StubCorporateActionService { ExceptionToThrow = new InvestmentRuleViolationException("A 1-for-1 split has no effect.") };
        var (viewModel, _, spy) = Build(service: service);

        var act = async () => await viewModel.Add(() => AsForm(ValidFormData()));

        await act.Should().NotThrowAsync();
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task AddCommand_ServerRejectsSplit_KeepsInlineFormOpenWithEnteredValuesAndInlineError()
    {
        var service = new StubCorporateActionService { ExceptionToThrow = new InvestmentRuleViolationException("A 1-for-1 split has no effect.") };
        var (viewModel, _, spy) = Build(service: service);

        viewModel.AddCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.FormViewModel != null);
        var formVm = viewModel.FormViewModel!;
        formVm.EffectiveDate = new DateTime(2026, 3, 1);
        formVm.RatioNumerator = 5m;
        formVm.RatioDenominator = 1m;
        formVm.Note = "typed note";

        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => !string.IsNullOrEmpty(formVm.ValidationMessage));

        // Critical retry-loop assertion: the SAME form instance stays open with everything the
        // user typed still intact - a server-side refusal must not discard entered values.
        viewModel.IsFormOpen.Should().BeTrue();
        viewModel.FormViewModel.Should().BeSameAs(formVm);
        formVm.ValidationMessage.Should().Be("A 1-for-1 split has no effect.");
        formVm.EffectiveDate.Should().Be(new DateTime(2026, 3, 1));
        formVm.RatioNumerator.Should().Be(5m);
        formVm.RatioDenominator.Should().Be(1m);
        formVm.Note.Should().Be("typed note");
        spy.AppliedDetails.Should().BeNull();
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCommand_RetryAfterRejectionSucceeds_ClosesFormAndAppliesDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCorporateActionService { ExceptionToThrow = new InvestmentRuleViolationException("A 1-for-1 split has no effect.") };
        var (viewModel, _, spy) = Build(service: service);

        viewModel.AddCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.FormViewModel != null);
        var formVm = viewModel.FormViewModel!;
        formVm.EffectiveDate = new DateTime(2026, 3, 1);
        formVm.RatioNumerator = 5m;
        formVm.RatioDenominator = 1m;
        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => !string.IsNullOrEmpty(formVm.ValidationMessage));

        service.ExceptionToThrow = null;
        service.AddSplitResult = expectedDetails;
        formVm.RatioNumerator = 4m;
        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => spy.AppliedDetails != null);

        viewModel.IsFormOpen.Should().BeFalse();
        viewModel.FormViewModel.Should().BeNull();
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Update_NullSelected_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Update(null, () => AsForm(ValidFormData()));

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCorporateActionService { UpdateSplitResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var record = new CorporateActionDTO { Id = id, Type = CorporateAction.CorporateActionType.Split, EffectiveDate = new DateTime(2025, 1, 1), RatioFactor = 2m };
        viewModel.Load("ctx", [record], AssetName);
        var selected = viewModel.CorporateActions.Single();

        await viewModel.Update(selected, () => AsForm(ValidFormData(id)));

        service.LastUpdateSplitRequest.Should().NotBeNull();
        service.LastUpdateSplitRequest!.Id.Should().Be(id);
        service.LastUpdateSplitRequest.RatioFactor.Should().Be(3m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Update_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.Empty, Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m },
            AssetName);

        await viewModel.Update(row, () => AsForm(ValidFormData()));

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Delete_NullSelected_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Delete(null, () => true);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_NotConfirmed_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var record = new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m };
        viewModel.Load("ctx", [record], AssetName);
        var selected = viewModel.CorporateActions.Single();

        await viewModel.Delete(selected, () => false);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_Confirmed_CallsServiceAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCorporateActionService { DeleteResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var record = new CorporateActionDTO { Id = id, Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m };
        viewModel.Load("ctx", [record], AssetName);
        var selected = viewModel.CorporateActions.Single();

        await viewModel.Delete(selected, () => true);

        service.LastDeleteRequest.Should().NotBeNull();
        service.LastDeleteRequest!.Id.Should().Be(id);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Delete_ServiceThrowsInvestmentRuleViolation_ShowsTheDomainMessageAndDoesNotCrashOrApplyDetails()
    {
        var service = new StubCorporateActionService { ExceptionToThrow = new InvestmentRuleViolationException("Cannot delete: a later corporate action depends on this one.") };
        var (viewModel, _, spy) = Build(service: service);
        var record = new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m };
        viewModel.Load("ctx", [record], AssetName);
        var selected = viewModel.CorporateActions.Single();

        var act = async () => await viewModel.Delete(selected, () => true);

        await act.Should().NotThrowAsync();
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning && m.Message == "Cannot delete: a later corporate action depends on this one.");
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public void DeleteCommand_WithEmptyIdParameter_ShowsWarningWithoutOpeningRealDialog()
    {
        var (viewModel, svc, spy) = Build();
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.Empty, Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m },
            AssetName);

        viewModel.DeleteCommand.Execute(row);

        viewModel.SelectedCorporateAction.Should().Be(row);
        svc.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public void AddCommand_CanExecute_FollowsHasContext()
    {
        var (viewModel, _, _) = Build(hasContext: false);

        viewModel.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void UpdateAndDeleteCommand_CanExecute_TrueForSplitRow()
    {
        var (viewModel, _, _) = Build();
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m },
            AssetName);

        viewModel.UpdateCommand.CanExecute(row).Should().BeTrue();
        viewModel.DeleteCommand.CanExecute(row).Should().BeTrue();
    }

    [Fact]
    public void UpdateAndDeleteCommand_CanExecute_TrueForMergerRow()
    {
        var (viewModel, _, _) = Build();
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Merger, EffectiveDate = DateTime.Today },
            AssetName);

        viewModel.UpdateCommand.CanExecute(row).Should().BeTrue();
        viewModel.DeleteCommand.CanExecute(row).Should().BeTrue();
    }

    [Fact]
    public void UpdateAndDeleteCommand_CanExecute_FalseForSpinOffRow()
    {
        var (viewModel, _, _) = Build();
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.SpinOff, EffectiveDate = DateTime.Today },
            AssetName);

        viewModel.UpdateCommand.CanExecute(row).Should().BeFalse();
        viewModel.DeleteCommand.CanExecute(row).Should().BeFalse();
    }

    [Fact]
    public async Task Add_Merger_Success_PassesCorrectRequestAndAppliesResolvedSourceAsset()
    {
        var sourceDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var targetDetails = new AssetDetailsDTO { Name = "Company B", BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T2" };
        var service = new StubCorporateActionService { AddMergerResult = new CorporateActionMergerResultDTO { Source = sourceDetails, Target = targetDetails } };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(MergerFormData()));

        service.LastAddMergerRequest.Should().NotBeNull();
        service.LastAddMergerRequest!.BrokerName.Should().Be(BrokerName);
        service.LastAddMergerRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastAddMergerRequest.SourceAssetName.Should().Be(AssetName);
        service.LastAddMergerRequest.TargetAssetName.Should().Be("Company B");
        service.LastAddMergerRequest.CreateTargetAssetInline.Should().BeTrue();
        service.LastAddMergerRequest.ExchangeRatio.Should().Be(2m);
        service.LastAddMergerRequest.CashInLieuAmount.Should().Be(5m);
        service.LastAddMergerRequest.Note.Should().Be("merger note");
        spy.AppliedDetails.Should().Be(sourceDetails);
        viewModel.IsFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Add_Merger_ExistingCandidateName_PassesCreateTargetAssetInlineFalse()
    {
        var service = new StubCorporateActionService
        {
            AddMergerResult = new CorporateActionMergerResultDTO
            {
                Source = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" }
            }
        };
        var (viewModel, _, _) = Build(service: service);

        await viewModel.Add(() => AsForm(MergerFormData(createTargetAssetInline: false)));

        service.LastAddMergerRequest!.CreateTargetAssetInline.Should().BeFalse();
    }

    [Fact]
    public async Task Add_Merger_ResolvesTargetSide_WhenCurrentAssetNameMatchesTargetName()
    {
        var sourceDetails = new AssetDetailsDTO { Name = "Company B", BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var targetDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T2" };
        var service = new StubCorporateActionService { AddMergerResult = new CorporateActionMergerResultDTO { Source = sourceDetails, Target = targetDetails } };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(MergerFormData()));

        spy.AppliedDetails.Should().Be(targetDetails);
    }

    [Fact]
    public async Task Update_Merger_PassesCorrectRequestAndAppliesResolvedAsset()
    {
        var id = Guid.NewGuid();
        var sourceDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCorporateActionService { UpdateMergerResult = new CorporateActionMergerResultDTO { Source = sourceDetails, Target = null } };
        var (viewModel, _, spy) = Build(service: service);
        var record = new CorporateActionDTO
        {
            Id = id,
            Type = CorporateAction.CorporateActionType.Merger,
            EffectiveDate = new DateTime(2025, 1, 1),
            Role = CorporateAction.CorporateActionRole.Source,
            ExchangeRatio = 3m
        };
        viewModel.Load("ctx", [record], AssetName);
        var selected = viewModel.CorporateActions.Single();

        await viewModel.Update(selected, () => AsForm(MergerFormData(id)));

        service.LastUpdateMergerRequest.Should().NotBeNull();
        service.LastUpdateMergerRequest!.Id.Should().Be(id);
        service.LastUpdateMergerRequest.SourceAssetName.Should().Be(AssetName);
        service.LastUpdateMergerRequest.ExchangeRatio.Should().Be(2m);
        service.LastUpdateMergerRequest.CashInLieuAmount.Should().Be(5m);
        spy.AppliedDetails.Should().Be(sourceDetails);
    }

    [Fact]
    public async Task AddCommand_ServerRejectsMerger_KeepsInlineFormOpenOnConfirmStepWithEnteredValuesAndInlineError()
    {
        var service = new StubCorporateActionService { ExceptionToThrow = new InvestmentRuleViolationException("An asset named 'Company B' already exists — select it or choose a different name.") };
        var assetAdminService = new StubAssetAdminService();
        var (viewModel, _, spy) = Build(service: service, assetAdminService: assetAdminService);

        viewModel.AddCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.FormViewModel != null);
        var formVm = viewModel.FormViewModel!;
        formVm.Type = "Merger";
        formVm.EffectiveDate = new DateTime(2026, 3, 1);
        formVm.TargetAssetPicker!.AssetName = "Company B";
        formVm.ExchangeRatio = 2m;
        formVm.CashInLieuAmount = 5m;

        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => formVm.IsMergerConfirmStep);
        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => !string.IsNullOrEmpty(formVm.ValidationMessage));

        viewModel.IsFormOpen.Should().BeTrue();
        viewModel.FormViewModel.Should().BeSameAs(formVm);
        formVm.IsMergerConfirmStep.Should().BeTrue();
        formVm.ValidationMessage.Should().Be("An asset named 'Company B' already exists — select it or choose a different name.");
        formVm.TargetAssetPicker!.AssetName.Should().Be("Company B");
        formVm.ExchangeRatio.Should().Be(2m);
        formVm.CashInLieuAmount.Should().Be(5m);
        spy.AppliedDetails.Should().BeNull();
        spy.Messages.Should().BeEmpty();
    }

    /// <summary>Polls rather than assumes synchronous continuation timing: the production code
    /// path under test runs through an `async void` command handler, whose continuations xUnit's
    /// own tracked SynchronizationContext may post rather than run inline.</summary>
    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }
            await Task.Delay(10);
        }
    }

    private sealed class Spy
    {
        public AssetDetailsDTO? AppliedDetails { get; private set; }
        public List<(string Message, string Caption, MessageBoxImage Image)> Messages { get; } = [];

        public void ApplyDetails(AssetDetailsDTO details) => AppliedDetails = details;
        public void ShowMessage(string message, string caption, MessageBoxImage image) => Messages.Add((message, caption, image));
    }

    private sealed class StubCorporateActionService : ICorporateActionService
    {
        public AssetDetailsDTO? AddSplitResult { get; set; }
        public AssetDetailsDTO? UpdateSplitResult { get; set; }
        public AssetDetailsDTO? DeleteResult { get; set; }
        public CorporateActionMergerResultDTO? AddMergerResult { get; set; }
        public CorporateActionMergerResultDTO? UpdateMergerResult { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public int AddMergerCallCount { get; private set; }
        public int UpdateMergerCallCount { get; private set; }
        public CorporateActionSplitCreateDTO? LastAddSplitRequest { get; private set; }
        public CorporateActionSplitUpdateDTO? LastUpdateSplitRequest { get; private set; }
        public CorporateActionDeleteDTO? LastDeleteRequest { get; private set; }
        public CorporateActionMergerCreateDTO? LastAddMergerRequest { get; private set; }
        public CorporateActionMergerUpdateDTO? LastUpdateMergerRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddSplitAsync(CorporateActionSplitCreateDTO request)
        {
            AddCallCount++;
            LastAddSplitRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(AddSplitResult);
        }

        public Task<AssetDetailsDTO?> UpdateSplitAsync(CorporateActionSplitUpdateDTO request)
        {
            UpdateCallCount++;
            LastUpdateSplitRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(UpdateSplitResult);
        }

        public Task<CorporateActionMergerResultDTO?> AddMergerAsync(CorporateActionMergerCreateDTO request)
        {
            AddMergerCallCount++;
            LastAddMergerRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<CorporateActionMergerResultDTO?>(ExceptionToThrow) : Task.FromResult(AddMergerResult);
        }

        public Task<CorporateActionMergerResultDTO?> UpdateMergerAsync(CorporateActionMergerUpdateDTO request)
        {
            UpdateMergerCallCount++;
            LastUpdateMergerRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<CorporateActionMergerResultDTO?>(ExceptionToThrow) : Task.FromResult(UpdateMergerResult);
        }

        public Task<CorporateActionSpinOffResultDTO?> AddSpinOffAsync(CorporateActionSpinOffCreateDTO request) => throw new NotSupportedException();
        public Task<CorporateActionSpinOffResultDTO?> UpdateSpinOffAsync(CorporateActionSpinOffUpdateDTO request) => throw new NotSupportedException();

        public Task<AssetDetailsDTO?> DeleteCorporateActionAsync(CorporateActionDeleteDTO request)
        {
            DeleteCallCount++;
            LastDeleteRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(DeleteResult);
        }
    }

    private sealed class StubAssetAdminService : IAssetAdminService
    {
        public IReadOnlyList<AssetAdminDTO> Assets { get; set; } = [];

        public IReadOnlyList<AssetAdminDTO> GetAssets() => Assets;

        public Task<AssetAdminDTO> CreateAssetAsync(AssetAdminCreateDTO request) => throw new NotSupportedException();

        public Task<AssetAdminDTO> UpdateAssetAsync(string brokerName, string portfolioName, string currentName, AssetAdminUpdateDTO request, InvestmentScope scope = InvestmentScope.Active) =>
            throw new NotSupportedException();
    }
}
