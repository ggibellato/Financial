using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class PriceHistoryTabViewModelTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    private static (PriceHistoryTabViewModel ViewModel, StubPriceService Service, Spy Spy) Build(
        bool hasContext = true,
        StubPriceService? service = null)
    {
        var stubService = service ?? new StubPriceService();
        var spy = new Spy();
        var viewModel = new PriceHistoryTabViewModel(
            stubService,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        return (viewModel, stubService, spy);
    }

    private static (PriceHistoryTabViewModel ViewModel, Spy Spy) BuildWithNullService(bool hasContext = true)
    {
        var spy = new Spy();
        var viewModel = new PriceHistoryTabViewModel(
            null,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        return (viewModel, spy);
    }

    private static PriceDialogData ValidDialogData(decimal price = 25m) => new(DateOnly.FromDateTime(DateTime.Today), price);

    private static Task<PriceDialogData?> AsForm(PriceDialogData? data) => Task.FromResult(data);

    [Fact]
    public async Task Set_NoContext_ShowsInfoAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build(hasContext: false);

        await viewModel.Set(() => AsForm(ValidDialogData()));

        service.SetCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Information);
    }

    [Fact]
    public async Task Set_NullService_DoesNotCallServiceOrShowMessage()
    {
        var (viewModel, spy) = BuildWithNullService();

        await viewModel.Set(() => AsForm(ValidDialogData()));

        spy.Messages.Should().BeEmpty();
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Set_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, spy) = Build();

        await viewModel.Set(() => AsForm(null));

        service.SetCallCount.Should().Be(0);
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Set_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubPriceService { SetResult = null };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Set(() => AsForm(ValidDialogData()));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Set_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubPriceService { SetResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var date = DateOnly.FromDateTime(DateTime.Today);

        await viewModel.Set(() => AsForm(new PriceDialogData(date, 42.5m)));

        service.LastSetRequest.Should().NotBeNull();
        service.LastSetRequest!.BrokerName.Should().Be(BrokerName);
        service.LastSetRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastSetRequest.AssetName.Should().Be(AssetName);
        service.LastSetRequest.Date.Should().Be(date);
        service.LastSetRequest.Price.Should().Be(42.5m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Delete_NullSelectedEntry_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Delete(null, () => true);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_NullService_DoesNotCallServiceOrShowMessage()
    {
        var (viewModel, spy) = BuildWithNullService();
        var selected = new AssetPriceSnapshotDTO { Date = DateOnly.FromDateTime(DateTime.Today), Price = 10m, IsManual = true };

        await viewModel.Delete(selected, () => true);

        spy.Messages.Should().BeEmpty();
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NotManual_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new AssetPriceSnapshotDTO { Date = DateOnly.FromDateTime(DateTime.Today), Price = 10m, IsManual = false };

        await viewModel.Delete(selected, () => true);

        service.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Delete_NotConfirmed_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var selected = new AssetPriceSnapshotDTO { Date = DateOnly.FromDateTime(DateTime.Today), Price = 10m, IsManual = true };

        await viewModel.Delete(selected, () => false);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubPriceService { DeleteResult = null };
        var (viewModel, _, spy) = Build(service: service);
        var selected = new AssetPriceSnapshotDTO { Date = DateOnly.FromDateTime(DateTime.Today), Price = 10m, IsManual = true };

        await viewModel.Delete(selected, () => true);

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubPriceService { DeleteResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var selected = new AssetPriceSnapshotDTO { Date = date, Price = 10m, IsManual = true };

        await viewModel.Delete(selected, () => true);

        service.LastDeleteRequest.Should().NotBeNull();
        service.LastDeleteRequest!.BrokerName.Should().Be(BrokerName);
        service.LastDeleteRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastDeleteRequest.AssetName.Should().Be(AssetName);
        service.LastDeleteRequest.Date.Should().Be(date);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    private sealed class Spy
    {
        public AssetDetailsDTO? AppliedDetails { get; private set; }
        public List<(string Message, string Caption, MessageBoxImage Image)> Messages { get; } = [];

        public void ApplyDetails(AssetDetailsDTO details) => AppliedDetails = details;
        public void ShowMessage(string message, string caption, MessageBoxImage image) => Messages.Add((message, caption, image));
    }

    private sealed class StubPriceService : IPriceService
    {
        public AssetDetailsDTO? SetResult { get; set; }
        public AssetDetailsDTO? DeleteResult { get; set; }
        public int SetCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public SetAssetPriceDTO? LastSetRequest { get; private set; }
        public DeleteAssetPriceDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request)
        {
            SetCallCount++;
            LastSetRequest = request;
            return Task.FromResult(SetResult);
        }

        public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request)
        {
            DeleteCallCount++;
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }

        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request) =>
            throw new NotSupportedException();
    }
}
