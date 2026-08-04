# Implementation Plan: F05. WPF Collapsed-Mode Flyouts & Tooltips

**Prerequisites:**
- F04 (WPF Sidebar Navigation Shell) implemented — `Sidebar.xaml`/`.xaml.cs`, `MainShellViewModel`, `NavTree.cs` already exist
- No new package references required

### Stage 1: Popup UI and Interaction Wiring

**1. Popup Markup** - Add a `Popup` per category to the sidebar's category template, anchored to that category's own header element, listing its children as clickable items styled consistently with the Expanded sidebar's own children list. Make category headers keyboard-focusable only while the sidebar is Collapsed.

**2. Popup State Machine** - Implement the code-behind logic that opens the popup belonging to a hovered or focused category header, keeps it open while the pointer or focus is within either the header or the popup, closes it after the mouse-leave delay or immediately on focus moving elsewhere, and closes it on Escape while returning keyboard focus to the triggering icon without reopening it.

### Stage 2: Verification

**3. Manual Verification** - Launch the app and confirm every behavior in the spec's manual verification checklist: exact child list and order on hover, click-to-select-and-close, the close delay and its re-entry tolerance, identical behavior via Tab-focus, Escape closing and refocusing without reopening, no popup while Expanded, and the toggle button's tooltip-only behavior.
