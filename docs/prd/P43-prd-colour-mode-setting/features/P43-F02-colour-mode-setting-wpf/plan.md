# Implementation Plan: F02. Colour Mode Setting (WPF)

**Prerequisites:**
- No new NuGet packages — WPF-UI 4.0.1 already provides `Wpf.Ui.Appearance.ApplicationThemeManager`, `ui:ControlsDictionary`, `ui:SymbolIcon`, and `ui:Button`.
- No dependency on F01 (Web) — the two front ends persist and apply their own colour mode independently, per the PRD's Out of Scope.

### Stage 1: Theme Bootstrap and Setting

**1. ColourMode Setting** - Add the new User-scoped `ColourMode` setting (default Light) to the settings file and its generated accessor, following the existing sidebar-collapsed setting's exact shape.

**2. Application-Wide Theme Resources** - Merge the WPF-UI controls dictionary once at the application level instead of per-view, and wire the startup path so the stored colour mode is applied before the main window is shown, with no visible flash.

**3. Shared Colour Mode ViewModel** - Add the singleton ViewModel that holds the current mode, exposes the two access points' bindable state and a toggle command, and applies/persists the mode through injected callbacks so it stays unit-testable. Register it in the DI container together with the startup-time theme application.

### Stage 2: Navigation, Appearance Page, and Header Shortcut

**4. Settings Navigation Entry** - Add the new Settings category with its single Appearance child to the navigation tree, positioned immediately after Admin, mirroring the existing category/child data shape.

**5. Appearance Page** - Add the Settings → Appearance view presenting the text-labelled Light/Dark control, bound to the shared ViewModel, and wire it into the shell's view registry and DI container.

**6. Header Shortcut Control** - Add the icon-button component showing the sun/moon icon and action-described accessible name for the opposite mode, bound to the same shared ViewModel, and mount it in the shell's always-visible header area next to the breadcrumb.

**7. Shell and ViewModel Tests** - Extend the navigation-tree and shell ViewModel test suites to cover the new category/child and the shell's exposure of the shared colour-mode state, and add the new ViewModel's own test suite covering mode switching, persistence, theme application, the toggle command, and the icon/accessible-name mapping, per the spec's testing strategy.

### Stage 3: Dark-Mode Audit and Verification

**8. Global Style Pass** - Update the application's shared grid and header styles to use theme-aware resources instead of hardcoded light-only colours, closing the largest share of the dark-mode legibility gap in one pass.

**9. Per-View Theme Merge Removal** - Remove the local, per-view light-theme merge from every identified view and dialog, keeping each file's own pinned brand-accent colours intact, so every view now follows the centralized theme instead of overriding it.

**10. Per-View Legibility Audit** - Walk each affected view against the spec's bounded checklist in Dark mode, adjusting any remaining hardcoded colour that fails the contrast/legibility check, with particular attention to the brand accent and any literal light-background elements.

**11. Manual Verification** - Launch the built app and confirm: Settings → Appearance is reachable in the correct nav position; a fresh install defaults to Light; selecting Dark re-themes the whole window with no restart; relaunching after choosing Dark shows no flash of Light; the header shortcut and the Appearance page always agree on the current mode; and the audited views read correctly in Dark, including the brand accent.
