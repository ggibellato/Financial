# Implementation Plan: F01. Colour Mode Setting (Web)

**Prerequisites:**
- No new npm packages — `@fluentui/react-components` (for `RadioGroup`/`Radio`) and `@fluentui/react-icons` (for `WeatherSunny24Regular`/`WeatherMoon24Regular`) are already dependencies

### Stage 1: Shared Colour Mode State

**1. Colour Mode Storage Util and Tests** - Add the `localStorage` get/set pair for the colour mode preference, defaulting to Light on any missing, invalid, or unreadable value, following the existing per-key persistence pattern. Write its test suite covering the default, the round-trip, and the defensive read/write failure paths.

**2. Colour Mode Context, Provider, and Tests** - Add the shared context/hook that both access points will read and write, initialized from the stored preference and persisting on every change, following the existing cross-component shared-state pattern in this codebase. Write its test suite covering the default value, initialization from a prior preference, updates, toggling, multi-consumer synchronization, and the outside-provider guard.

**3. Theme Wiring in the App Shell** - Wrap the app shell in the new provider, replace the OS-driven theme source with the shared context so the applied Fluent theme follows the stored preference instead of `prefers-color-scheme`, and remove the now-unused OS-listening hook. Extend the app shell's existing test suite to cover the default-Light-regardless-of-OS behavior and confirm the rest of the suite still passes unmodified.

### Stage 2: Settings Navigation and Appearance Page

**4. Settings Navigation Entry** - Add the "Settings" top-level category with its single "Appearance" leaf to the navigation data, positioned after "Admin", and wire the corresponding route and lazy-loaded page entry. Add the category's sidebar icon following the existing icon set's visual style.

**5. Appearance Page and Tests** - Add the page that presents the "Colour mode" control with its two text-labelled options, bound to the shared context so a selection updates the stored preference immediately. Write its test suite covering the control's shape, default and persisted selection, selecting an option, and staying in sync when the mode changes elsewhere.

### Stage 3: Header Shortcut

**6. Colour Mode Toggle Button and Tests** - Add the header icon-button that reads the shared context to show the correct icon and an action-described accessible name/tooltip, and toggles the mode on click. Mount it in the app shell's new top row alongside the breadcrumb. Write its test suite covering the icon/label for each mode, the toggle action, keyboard operability, and the accessible name.
