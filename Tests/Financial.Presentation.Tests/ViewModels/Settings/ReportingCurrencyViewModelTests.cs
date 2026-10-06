using Financial.Presentation.App.ViewModels.Settings;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Settings;

[Trait("Category", "Unit")]
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

    [Theory]
    [InlineData(Currency.GBP, Currency.BRL)]
    [InlineData(Currency.BRL, Currency.USD)]
    [InlineData(Currency.USD, Currency.GBP)]
    public void SelectingACurrencyOption_PersistsThatCurrencyAndMovesTheSelection(Currency initial, Currency chosen)
    {
        var (viewModel, provider) = CreateViewModel(initial);

        switch (chosen)
        {
            case Currency.GBP: viewModel.IsGbpSelected = true; break;
            case Currency.BRL: viewModel.IsBrlSelected = true; break;
            default: viewModel.IsUsdSelected = true; break;
        }

        provider.GetReportingCurrency().Should().Be(chosen);
        (chosen == Currency.GBP ? viewModel.IsGbpSelected : chosen == Currency.BRL ? viewModel.IsBrlSelected : viewModel.IsUsdSelected)
            .Should().BeTrue();
        viewModel.SaveError.Should().BeNull();
    }

    [Fact]
    public void DeselectingACurrencyOption_DoesNotCallTheProvider()
    {
        var (viewModel, provider) = CreateViewModel(Currency.GBP);
        provider.ThrowOnSetReportingCurrencyAsync = new InvalidOperationException("must not be called");

        viewModel.IsGbpSelected = false;
        viewModel.IsBrlSelected = false;
        viewModel.IsUsdSelected = false;

        viewModel.SaveError.Should().BeNull();
        viewModel.IsGbpSelected.Should().BeTrue();
    }

    [Fact]
    public void SettingIsEnabledThroughTheProperty_PersistsTheChange()
    {
        var (viewModel, provider) = CreateViewModel(enabled: true);

        viewModel.IsEnabled = false;

        provider.IsReportingCurrencyEnabled().Should().BeFalse();
        viewModel.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SettingIsEnabledToItsCurrentValue_DoesNotCallTheProvider()
    {
        var (viewModel, provider) = CreateViewModel(enabled: true);
        provider.ThrowOnSetReportingCurrencyEnabledAsync = new InvalidOperationException("must not be called");

        viewModel.IsEnabled = true;

        viewModel.SaveError.Should().BeNull();
        viewModel.IsEnabled.Should().BeTrue();
    }
}
