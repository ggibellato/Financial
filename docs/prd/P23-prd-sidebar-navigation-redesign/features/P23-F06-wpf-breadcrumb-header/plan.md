# Implementation Plan: F06. WPF Breadcrumb Header

**Prerequisites:**
- F04 (WPF Sidebar Navigation Shell) implemented — `MainShellViewModel`, `NavTree.cs` already exist
- No new package references required

### Stage 1: Breadcrumb Text and Shell Layout

**1. Breadcrumb Computed Property** - Add the computed breadcrumb text to the shell ViewModel, resolved from the currently selected item against the same navigation tree the sidebar renders from, falling back to an em dash when unmatched, with change notification firing whenever the selection changes. Cover it with unit tests against the spec's testing strategy, including the cross-feature check that its labels come from the same navigation tree the sidebar uses.

**2. Shell Layout Update** - Display the breadcrumb as a fixed-height bar above the routed content in the main window, alongside the sidebar, unaffected by its collapsed/expanded state.
