using Financial.Presentation.App.ViewModels.Settings;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Settings;

public class ReportingCurrencyViewModelTests
{
    private static (ReportingCurrencyViewModel ViewModel, StubReportingCurrencyProvider Provider) CreateViewModel(
        Currency initial = Currency.GBP, bool enabled = true)
    {
        var provider = new StubReportingCurrencyProvider(initial, enabled);
        var viewModel = new ReportingCurrencyViewModel(provider, new RecordingLogger<ReportingCurrencyViewModel>());
        return (viewModel, provider);
    }

    [Fact]
    public void Constructor_WithNullReportingCurrencyProvider_Throws()
    {
        Action act = () => new ReportingCurrencyViewModel(null!, new RecordingLogger<ReportingCurrencyViewModel>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("reportingCurrencyProvider");
    }

    [Fact]
    public void InitialSelection_ReflectsTheCurrentSetting()
    {
        var (viewModel, _) = CreateViewModel(Currency.BRL);

        viewModel.IsBrlSelected.Should().BeTrue();
        viewModel.IsGbpSelected.Should().BeFalse();
        viewModel.IsUsdSelected.Should().BeFalse();
    }

    [Fact]
    public async Task SettingIsBrlSelected_PersistsTheNewCurrencyAndUpdatesTheOtherOptions()
    {
        var (viewModel, provider) = CreateViewModel(Currency.GBP);

        await viewModel.SetCurrencyAsync(Currency.BRL);

        provider.GetReportingCurrency().Should().Be(Currency.BRL);
        viewModel.IsBrlSelected.Should().BeTrue();
        viewModel.IsGbpSelected.Should().BeFalse();
        viewModel.IsUsdSelected.Should().BeFalse();
        viewModel.SaveError.Should().BeNull();
    }

    [Fact]
    public async Task SettingTheSameCurrencyAgain_DoesNotCallTheProvider()
    {
        var (viewModel, provider) = CreateViewModel(Currency.GBP);

        await viewModel.SetCurrencyAsync(Currency.GBP);

        provider.GetReportingCurrency().Should().Be(Currency.GBP);
        viewModel.SaveError.Should().BeNull();
    }

    [Fact]
    public async Task SaveFailure_SurfacesSaveErrorAndKeepsTheLastKnownGoodSelection()
    {
        var (viewModel, provider) = CreateViewModel(Currency.GBP);
        provider.ThrowOnSetReportingCurrencyAsync = new InvalidOperationException("Currency not recognized");

        await viewModel.SetCurrencyAsync(Currency.BRL);

        viewModel.SaveError.Should().Be("Currency not recognized");
        viewModel.IsGbpSelected.Should().BeTrue();
        viewModel.IsBrlSelected.Should().BeFalse();
    }

    [Fact]
    public void InitialIsEnabled_ReflectsTheCurrentSetting()
    {
        var (viewModel, _) = CreateViewModel(enabled: false);

        viewModel.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SettingIsEnabledFalse_PersistsThroughTheProvider()
    {
        var (viewModel, provider) = CreateViewModel(enabled: true);

        await viewModel.SetEnabledAsync(false);

        provider.IsReportingCurrencyEnabled().Should().BeFalse();
        viewModel.IsEnabled.Should().BeFalse();
        viewModel.SaveError.Should().BeNull();
    }

    [Fact]
    public async Task SettingEnabledSaveFailure_SurfacesSaveErrorAndKeepsTheLastKnownGoodValue()
    {
        var (viewModel, provider) = CreateViewModel(enabled: true);
        provider.ThrowOnSetReportingCurrencyEnabledAsync = new InvalidOperationException("Save failed");

        await viewModel.SetEnabledAsync(false);

        viewModel.SaveError.Should().Be("Save failed");
        viewModel.IsEnabled.Should().BeTrue();
    }
}
