using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Exceptions;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionsTabViewModelTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    private static (TransactionsTabViewModel ViewModel, StubTransactionService Service, Spy Spy) Build(
        bool hasContext = true,
        ITransactionService? service = null,
        ITransactionQueryService? queryService = null)
    {
        var stubService = service as StubTransactionService ?? new StubTransactionService();
        var spy = new Spy();
        var viewModel = new TransactionsTabViewModel(
            stubService,
            queryService ?? new StubTransactionQueryService(),
            InvestmentScope.Active,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        return (viewModel, stubService, spy);
    }

    private static TransactionDialogData ValidDialogData(Guid? id = null) => new(
        TransactionId: id ?? Guid.NewGuid(),
        Date: DateTime.Today,
        Type: "Buy",
        Quantity: 10m,
        UnitPrice: 25m,
        Fees: 1.5m);

    private static Task<TransactionDialogData?> AsForm(TransactionDialogData? data) => Task.FromResult(data);

    [Fact]
    public async Task Add_NoContext_ShowsInfoAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build(hasContext: false);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Information);
    }

    [Fact]
    public async Task Add_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, spy) = Build();

        await viewModel.Add(() => AsForm(null));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();

        await viewModel.Add(() => AsForm(ValidDialogData() with { Type = "NotAType" }));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Add_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { AddResult = null };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Add_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { AddResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        service.LastAddRequest.Should().NotBeNull();
        service.LastAddRequest!.BrokerName.Should().Be(BrokerName);
        service.LastAddRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastAddRequest.AssetName.Should().Be(AssetName);
        service.LastAddRequest.Type.Should().Be("Buy");
        service.LastAddRequest.Quantity.Should().Be(10m);

        service.LastAddRequest.UnitPrice.Should().Be(25m);
        service.LastAddRequest.Fees.Should().Be(1.5m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Add_ServiceThrowsInvestmentRuleViolation_ShowsTheDomainMessageAndDoesNotCrashOrApplyDetails()
    {
        var service = new StubTransactionService { ExceptionToThrow = new InvestmentRuleViolationException("Cannot sell 15 units on 2024-02-01 — only 10 were held on that date.") };
        var (viewModel, _, spy) = Build(service: service);
        viewModel.Load("ctx", [new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m }]);

        var act = async () => await viewModel.Add(() => AsForm(ValidDialogData()));

        await act.Should().NotThrowAsync();
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning && m.Message == "Cannot sell 15 units on 2024-02-01 — only 10 were held on that date.");
        spy.AppliedDetails.Should().BeNull();
        viewModel.Transactions.Should().ContainSingle();
    }

    [Fact]
    public async Task AddTransactionCommand_ServerRejectsOversell_KeepsInlineFormOpenWithEnteredValuesAndInlineError()
    {
        var service = new StubTransactionService { ExceptionToThrow = new InvestmentRuleViolationException("Cannot sell 15 units on 2024-02-01 — only 10 were held on that date.") };
        var (viewModel, _, spy) = Build(service: service);
        viewModel.Load("ctx", []);

        viewModel.AddTransactionCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TransactionFormViewModel != null);
        var formVm = viewModel.TransactionFormViewModel!;
        formVm.Type = "Sell";
        formVm.Quantity = 15m;
        formVm.UnitPrice = 10m;
        formVm.Date = new DateTime(2024, 2, 1);

        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => !string.IsNullOrEmpty(formVm.ValidationMessage));

        // The refusal must not discard what was typed: the same inline form stays open, on the
        // same view-model instance, with the server's message surfacing where client-side
        // validation already does - not via a disconnected MessageBox that leaves nothing to retry.
        viewModel.IsTransactionFormOpen.Should().BeTrue();
        viewModel.TransactionFormViewModel.Should().BeSameAs(formVm);
        formVm.ValidationMessage.Should().Be("Cannot sell 15 units on 2024-02-01 — only 10 were held on that date.");
        formVm.Quantity.Should().Be(15m);
        formVm.UnitPrice.Should().Be(10m);
        spy.AppliedDetails.Should().BeNull();
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTransactionCommand_RetryAfterRejectionSucceeds_ClosesFormAndAppliesDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { ExceptionToThrow = new InvestmentRuleViolationException("Cannot sell 15 units on 2024-02-01 — only 10 were held on that date.") };
        var (viewModel, _, spy) = Build(service: service);
        viewModel.Load("ctx", []);

        viewModel.AddTransactionCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TransactionFormViewModel != null);
        var formVm = viewModel.TransactionFormViewModel!;
        formVm.Type = "Sell";
        formVm.Quantity = 15m;
        formVm.UnitPrice = 10m;
        formVm.Date = new DateTime(2024, 2, 1);
        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => !string.IsNullOrEmpty(formVm.ValidationMessage));

        service.ExceptionToThrow = null;
        service.AddResult = expectedDetails;
        formVm.Quantity = 5m;
        formVm.ConfirmCommand.Execute(null);
        await WaitUntilAsync(() => spy.AppliedDetails != null);

        viewModel.IsTransactionFormOpen.Should().BeFalse();
        viewModel.TransactionFormViewModel.Should().BeNull();
        spy.AppliedDetails.Should().Be(expectedDetails);
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

    [Fact]
    public async Task Add_ServiceThrowsUnexpectedException_ShowsGenericMessageAndDoesNotCrash()
    {
        var service = new StubTransactionService { ExceptionToThrow = new InvalidOperationException("boom") };
        var (viewModel, _, spy) = Build(service: service);

        var act = async () => await viewModel.Add(() => AsForm(ValidDialogData()));

        await act.Should().NotThrowAsync();
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning && m.Message == "Transaction could not be added. Check the values and try again.");
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task AddTransactionCommand_AfterSuccessfulAdd_PersistsDateAndTypeForNextOpen()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { AddResult = expectedDetails };
        var (viewModel, _, _) = Build(service: service);
        var usedDate = DateTime.Today.AddDays(-3);

        await viewModel.Add(() => AsForm(ValidDialogData() with { Date = usedDate, Type = "Sell" }));

        viewModel.AddTransactionCommand.Execute(null);

        viewModel.TransactionFormViewModel!.Date.Should().Be(usedDate);
        viewModel.TransactionFormViewModel!.Type.Should().Be("Sell");
    }

    [Fact]
    public async Task Update_NullSelectedTransaction_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Update(null, () => AsForm(ValidDialogData()));

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData()));

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(null));

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(selected.Id) with { Type = "NotAType" }));

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { UpdateResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(id) with { Type = "Sell", Quantity = 3m }));

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Id.Should().Be(id);
        service.LastUpdateRequest.Type.Should().Be("Sell");
        service.LastUpdateRequest.Quantity.Should().Be(3m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Update_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { UpdateResult = null };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(id)));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Update_ServiceThrowsInvestmentRuleViolation_ShowsTheDomainMessageAndDoesNotCrashOrApplyDetails()
    {
        var service = new StubTransactionService { ExceptionToThrow = new InvestmentRuleViolationException("This change would leave the sale of 80 units on 2024-06-01 short by 30 units — only 50 would be held on that date.") };
        var (viewModel, _, spy) = Build(service: service);
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 100m, UnitPrice = 5m, Fees = 0m };

        var act = async () => await viewModel.Update(selected, () => AsForm(ValidDialogData(selected.Id)));

        await act.Should().NotThrowAsync();
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning && m.Message == "This change would leave the sale of 80 units on 2024-06-01 short by 30 units — only 50 would be held on that date.");
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NullSelectedTransaction_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Delete(null, () => true);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        service.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Delete_NotConfirmed_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => false);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_Success_PassesCorrectIdAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { DeleteResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        service.LastDeleteRequest.Should().NotBeNull();
        service.LastDeleteRequest!.Id.Should().Be(id);
        service.LastDeleteRequest.BrokerName.Should().Be(BrokerName);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Delete_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { DeleteResult = null };
        var (viewModel, _, spy) = Build(service: service);
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Add_NullTransactionService_ReturnsWithoutThrowingOrShowingMessage()
    {
        var spy = new Spy();
        var viewModel = new TransactionsTabViewModel(
            null,
            new StubTransactionQueryService(),
            InvestmentScope.Active,
            () => true,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        spy.Messages.Should().BeEmpty();
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NullTransactionService_ReturnsWithoutThrowingOrShowingMessage()
    {
        var spy = new Spy();
        var viewModel = new TransactionsTabViewModel(
            null,
            new StubTransactionQueryService(),
            InvestmentScope.Active,
            () => true,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        spy.Messages.Should().BeEmpty();
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ServiceThrowsInvestmentRuleViolation_ShowsTheDomainMessageAndDoesNotCrashOrApplyDetails()
    {
        var service = new StubTransactionService { ExceptionToThrow = new InvestmentRuleViolationException("This change would leave the sale of 10 units on 2024-02-01 short by 10 units — only 0 would be held on that date.") };
        var (viewModel, _, spy) = Build(service: service);
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m };

        var act = async () => await viewModel.Delete(selected, () => true);

        await act.Should().NotThrowAsync();
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public void Properties_ExposeExpectedDefaultsAndCommands()
    {
        var (viewModel, _, _) = Build();

        viewModel.SelectedTransaction.Should().BeNull();
        viewModel.IsTransactionFormOpen.Should().BeFalse();
        viewModel.HasTransactionsError.Should().BeFalse();
        viewModel.UpdateTransactionCommand.Should().NotBeNull();
        viewModel.DeleteTransactionCommand.Should().NotBeNull();
        viewModel.AddTransactionCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddTransactionCommand_CanExecute_FollowsHasContext()
    {
        var (viewModel, _, _) = Build(hasContext: false);

        viewModel.AddTransactionCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Load_WithTransactions_BuildsPlotModelAndUpdatePlotWidthAppliesLabelDensity()
    {
        var (viewModel, _, _) = Build();
        var transactions = new List<TransactionDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m },
        };

        viewModel.Load("ctx", transactions);

        viewModel.Transactions.Should().ContainSingle();
        viewModel.SelectedTransaction.Should().BeNull();

        var act = () => viewModel.UpdatePlotWidth(400);
        act.Should().NotThrow();
    }

    [Fact]
    public void UpdatePlotWidth_WithNonPositiveWidth_IsNoOp()
    {
        var (viewModel, _, _) = Build();
        viewModel.Load("ctx", [new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m }]);

        var act = () => viewModel.UpdatePlotWidth(0);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task LoadPortfolio_QueryServiceThrows_SetsErrorAndStopsLoading()
    {
        var queryService = new StubTransactionQueryService { ExceptionToThrow = new InvalidOperationException("boom") };
        var (viewModel, _, _) = Build(queryService: queryService);

        await viewModel.LoadPortfolio(BrokerName, PortfolioName);

        viewModel.TransactionsError.Should().Be("Unable to load transactions");
        viewModel.HasTransactionsError.Should().BeTrue();
        viewModel.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBroker_QueryServiceThrows_SetsErrorAndStopsLoading()
    {
        var queryService = new StubTransactionQueryService { ExceptionToThrow = new InvalidOperationException("boom") };
        var (viewModel, _, _) = Build(queryService: queryService);

        await viewModel.LoadBroker(BrokerName);

        viewModel.TransactionsError.Should().Be("Unable to load transactions");
        viewModel.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public void UpdateTransactionCommand_WithParameterAndConfirmedForm_SelectsTransactionOpensFormAndCallsService()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { UpdateResult = expectedDetails };
        var (viewModel, svc, spy) = Build(service: service);
        var tx = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m };

        viewModel.UpdateTransactionCommand.Execute(tx);

        viewModel.SelectedTransaction.Should().Be(tx);
        viewModel.IsTransactionFormOpen.Should().BeTrue();
        viewModel.TransactionFormViewModel.Should().NotBeNull();

        viewModel.TransactionFormViewModel!.ConfirmCommand.Execute(null);

        viewModel.IsTransactionFormOpen.Should().BeFalse();
        viewModel.TransactionFormViewModel.Should().BeNull();
        svc.UpdateCallCount.Should().Be(1);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public void UpdateTransactionCommand_WithParameterAndCancelledForm_DoesNotCallService()
    {
        var (viewModel, svc, _) = Build();
        var tx = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m };

        viewModel.UpdateTransactionCommand.Execute(tx);
        viewModel.TransactionFormViewModel!.CancelCommand.Execute(null);

        viewModel.IsTransactionFormOpen.Should().BeFalse();
        svc.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void DeleteTransactionCommand_WithEmptyIdParameter_SelectsTransactionAndShowsWarningWithoutOpeningRealDialog()
    {
        var (viewModel, svc, spy) = Build();
        var tx = new TransactionDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 5m, Fees = 0m };

        viewModel.DeleteTransactionCommand.Execute(tx);

        viewModel.SelectedTransaction.Should().Be(tx);
        svc.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    private sealed class Spy
    {
        public AssetDetailsDTO? AppliedDetails { get; private set; }
        public List<(string Message, string Caption, MessageBoxImage Image)> Messages { get; } = [];

        public void ApplyDetails(AssetDetailsDTO details) => AppliedDetails = details;
        public void ShowMessage(string message, string caption, MessageBoxImage image) => Messages.Add((message, caption, image));
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public AssetDetailsDTO? AddResult { get; set; }
        public AssetDetailsDTO? UpdateResult { get; set; }
        public AssetDetailsDTO? DeleteResult { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public TransactionCreateDTO? LastAddRequest { get; private set; }
        public TransactionUpdateDTO? LastUpdateRequest { get; private set; }
        public TransactionDeleteDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request)
        {
            AddCallCount++;
            LastAddRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(AddResult);
        }

        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request)
        {
            UpdateCallCount++;
            LastUpdateRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(UpdateResult);
        }

        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request)
        {
            DeleteCallCount++;
            LastDeleteRequest = request;
            return ExceptionToThrow is not null ? Task.FromException<AssetDetailsDTO?>(ExceptionToThrow) : Task.FromResult(DeleteResult);
        }
    }
}
