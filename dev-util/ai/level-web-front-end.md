## Objective

Create a Product Requirements Document (PRD) for bringing the React Web Front End to feature and user experience parity with the existing WPF Front End.

The Web Front End is already functional but does not currently provide the same user experience, layout, navigation, and behavior as the WPF application.

The goal of this work is to make the Web Front End behave as closely as possible to the WPF Front End while maintaining appropriate web implementation patterns.

The WPF application should be treated as the source of truth for functionality and user experience.

---

## General Requirements

### Navigation

The WPF application uses three top-level tabs:

1. Portfolio Navigator
2. Shares Dividend Check
3. Read Assets Current Values

The Web Front End should provide equivalent navigation, preferably using a top navigation bar.

Switching between these sections should completely change the main content area, similar to the behavior in WPF.

### Layout

The application should occupy the full browser viewport.

Requirements:

* No page-level scrolling.
* The browser window should not display vertical or horizontal scrollbars during normal usage.
* Individual controls and panels may have their own internal scrollbars when required.
* Layout should remain usable across common desktop resolutions.

---

# Portfolio Navigator

This is the most complex area of the application.

Reference screenshot:

`dev-util/ai/wpf-screenshots/navigator.png`

## Layout

The screen is divided into two primary areas:

### Investments Panel

Located on the left side.

Responsibilities:

* Display investment hierarchy.
* Allow navigation through:

  * Brokers
  * Portfolios
  * Assets

Requirements:

* Tree-based navigation.
* Expand/collapse support.
* Independent scrolling.
* Width should be adjustable if practical.
* Selection state must be clearly visible.

### Details Panel

Located on the right side.

This area changes based on the currently selected Broker, Portfolio, or Asset.

The panel contains three sub-sections:

1. Summary
2. Transactions
3. Credits

These may be implemented as tabs or a secondary navigation bar.

---

## Summary View

Reference screenshot:

`dev-util/ai/wpf-screenshots/navigator.png`

The displayed information varies based on the selected node.

### Broker Selected

Display aggregated information for all portfolios and assets within the broker.

Example:

* Total Credits = sum of all credits across all portfolios and assets belonging to the broker.

### Portfolio Selected

Display aggregated information for all assets within the portfolio.

Example:

* Total Credits = sum of all credits across all assets belonging to the portfolio.

### Asset Selected

Display information specific to the selected asset.

Example:

* Total Credits = total credits for that asset.

The PRD should identify all summary fields currently available in the WPF implementation and ensure they are replicated in the Web Front End.

---

## Transactions View

Reference screenshot:

`dev-util/ai/wpf-screenshots/transactions.png`

Requirements:

* Only available when an Asset is selected.
* Display transaction history for the selected asset.
* Match the WPF presentation and behavior as closely as possible.
* Preserve sorting, formatting, and calculations currently present in WPF.

When Broker or Portfolio is selected, define expected behavior based on the existing WPF implementation.

---

## Credits View

The Credits section behaves differently depending on the selected item.

### Broker Selected

Reference screenshot:

`dev-util/ai/wpf-screenshots/credits1.png`

Requirements:

* Display credits graph only.

### Portfolio Selected

Reference screenshot:

`dev-util/ai/wpf-screenshots/credits1.png`

Requirements:

* Display credits graph only.

### Asset Selected

Reference screenshot:

`dev-util/ai/wpf-screenshots/credits2.png`

Requirements:

* Display credits graph.
* Display credits list.
* Match WPF behavior and calculations.

The PRD should document all graph interactions, filtering options, formatting rules, and calculations currently implemented in WPF.

---

# Shares Dividend Check

Reference screenshots:

* `dev-util/ai/wpf-screenshots/shares.png`
* `dev-util/ai/wpf-screenshots/shares-combobox.png`

Requirements:

* Replicate the WPF screen in the Web Front End.
* Preserve all existing functionality.
* Provide the same initial list of pre-populated ticker symbols.
* Match displayed data, calculations, formatting, and interactions.
* Match loading and refresh behavior.

The PRD should document all user interactions and expected outputs.

---

# Read Assets Current Values

Reference screenshot:

`dev-util/ai/wpf-screenshots/current.png`

Requirements:

* Replicate the WPF screen in the Web Front End.
* Display current values for owned assets.
* Preserve the existing pre-populated asset list used by the WPF implementation.
* Match data presentation, calculations, formatting, and interactions.

The PRD should fully document the expected behavior and data flow.

---

# Expected Deliverables

The generated PRD should include:

1. Functional Requirements
2. User Stories
3. Acceptance Criteria
4. UI/UX Requirements
5. Navigation Requirements
6. Layout Requirements
7. Responsive Behavior Requirements
8. Component Breakdown
9. Data Requirements
10. Gap Analysis Between Current Web and WPF Implementations
11. Implementation Plan
12. Testing Requirements

The PRD should be detailed enough that an AI coding agent can implement the required changes without needing to inspect the WPF application manually beyond the provided screenshots and existing source code.
