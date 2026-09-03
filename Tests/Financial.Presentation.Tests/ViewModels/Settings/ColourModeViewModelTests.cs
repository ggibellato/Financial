using Financial.Presentation.App.ViewModels.Settings;
using FluentAssertions;
using Wpf.Ui.Controls;

namespace Financial.Presentation.Tests.ViewModels.Settings;

public class ColourModeViewModelTests
{
    private static ColourModeViewModel Create(
        ColourMode initialMode = ColourMode.Light,
        Action<ColourMode>? persist = null,
        Action<ColourMode>? applyTheme = null) =>
        new(initialMode, applyTheme ?? (_ => { }), persist ?? (_ => { }));

    [Fact]
    public void Constructor_WithNullPersist_Throws()
    {
        Action act = () => new ColourModeViewModel(ColourMode.Light, _ => { }, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("persist");
    }

    [Fact]
    public void Constructor_WithNullApplyTheme_Throws()
    {
        Action act = () => new ColourModeViewModel(ColourMode.Light, null!, _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("applyTheme");
    }

    [Fact]
    public void Constructor_AppliesInitialThemeImmediately()
    {
        var applied = new List<ColourMode>();

        Create(ColourMode.Dark, applyTheme: applied.Add);

        applied.Should().Equal(ColourMode.Dark);
    }

    [Fact]
    public void Constructor_WithInitialModeLight_IsLightSelectedIsTrue()
    {
        var vm = Create(ColourMode.Light);

        vm.IsLightSelected.Should().BeTrue();
        vm.IsDarkSelected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithInitialModeDark_IsDarkSelectedIsTrue()
    {
        var vm = Create(ColourMode.Dark);

        vm.IsDarkSelected.Should().BeTrue();
        vm.IsLightSelected.Should().BeFalse();
    }

    [Fact]
    public void SetIsDarkSelectedTrue_FromLight_SwitchesModePersistsAndAppliesTheme()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Light, persist: persisted.Add, applyTheme: applied.Add);

        vm.IsDarkSelected = true;

        vm.Mode.Should().Be(ColourMode.Dark);
        persisted.Should().Equal(ColourMode.Dark);
        applied.Should().Equal(ColourMode.Light, ColourMode.Dark);
    }

    [Fact]
    public void SetIsLightSelectedTrue_FromDark_SwitchesModePersistsAndAppliesTheme()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Dark, persist: persisted.Add, applyTheme: applied.Add);

        vm.IsLightSelected = true;

        vm.Mode.Should().Be(ColourMode.Light);
        persisted.Should().Equal(ColourMode.Light);
        applied.Should().Equal(ColourMode.Dark, ColourMode.Light);
    }

    [Fact]
    public void SetIsDarkSelectedFalse_IsIgnored()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Light, persist: persisted.Add, applyTheme: applied.Add);

        vm.IsDarkSelected = false;

        vm.Mode.Should().Be(ColourMode.Light);
        persisted.Should().BeEmpty();
        applied.Should().Equal(ColourMode.Light);
    }

    [Fact]
    public void SetIsLightSelectedTrue_WhenAlreadyLight_IsNoOp()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Light, persist: persisted.Add, applyTheme: applied.Add);

        vm.IsLightSelected = true;

        persisted.Should().BeEmpty();
        applied.Should().Equal(ColourMode.Light);
    }

    [Fact]
    public void ToggleCommand_FromLight_SwitchesToDark()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Light, persist: persisted.Add, applyTheme: applied.Add);

        vm.ToggleCommand.Execute(null);

        vm.Mode.Should().Be(ColourMode.Dark);
        persisted.Should().Equal(ColourMode.Dark);
        applied.Should().Equal(ColourMode.Light, ColourMode.Dark);
    }

    [Fact]
    public void ToggleCommand_FromDark_SwitchesToLight()
    {
        var persisted = new List<ColourMode>();
        var applied = new List<ColourMode>();
        var vm = Create(ColourMode.Dark, persist: persisted.Add, applyTheme: applied.Add);

        vm.ToggleCommand.Execute(null);

        vm.Mode.Should().Be(ColourMode.Light);
        persisted.Should().Equal(ColourMode.Light);
        applied.Should().Equal(ColourMode.Dark, ColourMode.Light);
    }

    [Fact]
    public void ToggleIcon_ReflectsCurrentMode()
    {
        var vm = Create(ColourMode.Light);
        vm.ToggleIcon.Should().Be(SymbolRegular.WeatherMoon24);

        vm.ToggleCommand.Execute(null);
        vm.ToggleIcon.Should().Be(SymbolRegular.WeatherSunny24);
    }

    [Fact]
    public void ToggleAutomationName_DescribesActionNotState()
    {
        var vm = Create(ColourMode.Light);
        vm.ToggleAutomationName.Should().Contain("Switch to Dark mode");

        vm.ToggleCommand.Execute(null);
        vm.ToggleAutomationName.Should().Contain("Switch to Light mode");
    }

    [Fact]
    public void PropertyChanged_RaisedForModeDependentProperties()
    {
        var vm = Create(ColourMode.Light);
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        vm.ToggleCommand.Execute(null);

        raised.Should().Contain(nameof(ColourModeViewModel.IsLightSelected));
        raised.Should().Contain(nameof(ColourModeViewModel.IsDarkSelected));
        raised.Should().Contain(nameof(ColourModeViewModel.ToggleIcon));
        raised.Should().Contain(nameof(ColourModeViewModel.ToggleAutomationName));
    }
}
