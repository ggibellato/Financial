# Implementation Plan: F03. Payment Due Banner (WPF)

**Prerequisites:**
- F01 and F02 merged (`IPaymentsDueService`/`PaymentDueDTO` already registered and available in-process to `Financial.App` via `AddFinancialCashFlowApplication()`)
- No new NuGet packages — WPF-UI 4.0.1 already provides `SymbolIcon`/`Button`

### Stage 1: ViewModels and Shell Wiring

**1. Payment Due Row and Banner ViewModels** - Add the row-level ViewModel that maps one payment into display-ready properties (type label, name, formatted date, days-remaining text, urgency brush/icon/accessible-label), and the banner ViewModel that fetches the payments-due list once at construction, exposes the row list and visibility, and owns the 10-second auto-dismiss timer plus a dismiss command. Reference the spec for the exact urgency-tier mapping and timer behavior.

**2. DI Registration and Shell Composition** - Register the new banner ViewModel in the WPF DI container, add it as a constructor dependency of the main shell ViewModel and window, and pass it through the same way the existing sync-status indicator is wired.

### Stage 2: ViewModel Tests

**3. Test Double** - Add a hand-written stub implementation of the payments-due service to the shared WPF test utilities, following the existing stub pattern used by other ViewModel tests.

**4. ViewModel Tests** - Write the test suite covering fetch-once behavior, empty/non-empty visibility, row mapping and ordering, dismiss behavior, and the urgency-tier-to-brush/icon/label mapping, per the spec's testing strategy.

### Stage 3: Banner UI and Verification

**5. Payment Due Banner UserControl** - Add the WPF UserControl rendering the banner: title, close button, and one row per payment with its urgency icon/color and details, mirroring the existing sync-status indicator's visibility pattern.

**6. Mount in Shell Chrome** - Add the new control to the main window's XAML next to the existing sync-status indicator.

**7. Manual Verification** - Launch the built app and confirm the banner appears with real qualifying data, auto-dismisses after 10 seconds, dismisses immediately on manual close, and matches the already-shipped Web banner's content and ordering for the same data.
