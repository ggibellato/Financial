# Implementation Plan: F04. WPF Sidebar Navigation Shell

**Prerequisites:**
- `Financial.App` WPF app (.NET, MVVM with `ViewModelBase`/`RelayCommand`), `Financial.Presentation.Tests` (xUnit + FluentAssertions) already configured
- No new package references required (`Settings.settings` support is already provided by the WPF shared framework)

### Stage 1: Navigation Data and Shell State

**1. Navigation Tree Data Module** - Create the shared, view-instance-free navigation tree data module defining the two categories and their ordered children (each carrying the key used to look up its destination view). Cover it with data-shape tests confirming the category/child counts, order, and unique view keys against the current `MainWindow.xaml` tab layout.

**2. MainShellViewModel** - Build the ViewModel that owns the collapsed/expanded state, the currently selected child, and the currently displayed content, exposing a toggle command and a select-item command. Persistence is delegated to an injected callback rather than a direct settings dependency, keeping the ViewModel unit-testable. Cover it with unit tests against the spec's testing strategy, including the default-selection, toggle, and item-selection behaviors.

### Stage 2: Persisted Setting and Sidebar UI

**3. Sidebar Collapsed-State Setting** - Add the new user-scoped setting that persists the sidebar's collapsed/expanded state across app restarts, following the project's existing (currently unused) settings scaffold conventions.

**4. Sidebar Component** - Build the `Sidebar` UserControl: the toggle button, the two category sections with their children, active-item highlighting, and the active-category icon tint, all bound to `MainShellViewModel` inherited via `DataContext`.

**5. Sidebar Width and Highlighting Converters** - Add the converters needed to bind the sidebar's column width to its collapsed state and to determine whether a category should show its active tint, following the project's existing converter-registration pattern in `App.xaml`.

### Stage 3: Shell Integration

**6. MainWindow Restructuring** - Replace the two nested `TabControl`s in `MainWindow.xaml` with the `Sidebar` and a `ContentControl` bound to the shell's selected content. Update `MainWindow.xaml.cs` to build the two investment views directly in code (matching how the other eight are already provided), assemble the full view map, read the persisted collapsed state, construct the shell ViewModel with its persistence callback, and set it as the window's `DataContext`. Remove the now-unused public view-model properties this replaces.

**7. Manual Verification** - Launch the app and confirm: the sidebar renders Expanded by default, toggling collapses/expands and the content column reflows, the setting persists and is honored across a restart with no visible flash, the active item and its category icon are highlighted, category headers don't change the selection, and all ten destination views still display their existing, unmodified content when selected.
