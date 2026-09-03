using Wpf.Ui.Controls;

namespace Financial.Presentation.App.ViewModels.Settings;

public enum ColourMode
{
    Light,
    Dark,
}

/// <summary>
/// Single source of truth for the app's Light/Dark colour mode, shared unmodified by both
/// access points (the Settings &gt; Appearance page and the header shortcut button) so the two
/// can never disagree. Applying and persisting the mode are delegated to injected callbacks,
/// mirroring <see cref="MainShellViewModel"/>'s <c>persistCollapsed</c> pattern, so this
/// ViewModel stays unit-testable without touching <see cref="Properties.Settings"/> or
/// <c>Wpf.Ui.Appearance.ApplicationThemeManager</c> directly.
/// </summary>
public class ColourModeViewModel : ViewModelBase
{
    private readonly Action<ColourMode> _applyTheme;
    private readonly Action<ColourMode> _persist;
    private ColourMode _mode;

    public ColourModeViewModel(ColourMode initialMode, Action<ColourMode> applyTheme, Action<ColourMode> persist)
    {
        _applyTheme = applyTheme ?? throw new ArgumentNullException(nameof(applyTheme));
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        _mode = initialMode;

        ToggleCommand = new RelayCommand(Toggle);

        _applyTheme(_mode);
    }

    public RelayCommand ToggleCommand { get; }

    public ColourMode Mode
    {
        get => _mode;
        private set
        {
            if (SetProperty(ref _mode, value))
            {
                OnPropertyChanged(nameof(IsLightSelected));
                OnPropertyChanged(nameof(IsDarkSelected));
                OnPropertyChanged(nameof(ToggleIcon));
                OnPropertyChanged(nameof(ToggleAutomationName));
            }
        }
    }

    public bool IsLightSelected
    {
        get => Mode == ColourMode.Light;
        set
        {
            if (value)
            {
                SetMode(ColourMode.Light);
            }
        }
    }

    public bool IsDarkSelected
    {
        get => Mode == ColourMode.Dark;
        set
        {
            if (value)
            {
                SetMode(ColourMode.Dark);
            }
        }
    }

    /// <summary>Shows the icon for the mode a click would switch TO, not the current mode.</summary>
    public SymbolRegular ToggleIcon => Mode == ColourMode.Light ? SymbolRegular.WeatherMoon24 : SymbolRegular.WeatherSunny24;

    /// <summary>Describes the action a click performs, not the current state.</summary>
    public string ToggleAutomationName => Mode == ColourMode.Light ? "Switch to Dark mode" : "Switch to Light mode";

    private void Toggle()
    {
        SetMode(Mode == ColourMode.Light ? ColourMode.Dark : ColourMode.Light);
    }

    private void SetMode(ColourMode mode)
    {
        if (mode == Mode)
        {
            return;
        }

        Mode = mode;
        _persist(mode);
        _applyTheme(mode);
    }
}
