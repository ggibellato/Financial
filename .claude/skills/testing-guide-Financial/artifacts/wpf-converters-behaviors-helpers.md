> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# WPF Converters, Behaviors, Helpers, Input and Navigation (`Financial.App/{Converters,Behaviors,Helpers,Input,Navigation}/*.cs`)

## What to test

- **`IValueConverter.Convert`** for each input class: the mapped value, `null`,
  `DependencyProperty.UnsetValue`, an unexpected type, and the `parameter` variants
  (`DateFormatConverter` with and without a format string). `ConvertBack` only where implemented.
- **Colour/brush converters** (`BillStatusToBrushConverter`, `SignedValueToBrushConverter`,
  `PositionTypeToColorConverter`, `TransactionTypeToColorConverter`): each status maps to the
  expected semantic brush **and** the view carries a non-colour cue (text/icon) — the
  colour-alone rule in `docs/ui/accessibility.md` is asserted on the ViewModel/XAML side, not
  here.
- **Behaviors**: `SortCycle` (none → asc → desc → none), `NullLastComparer` (nulls last in both
  directions, mixed types), `DecimalInputBehavior` via `DecimalInputHelper` (culture decimal
  separator, rejected characters, paste).
- **Helpers**: `DateFormatHelper.GetPaddedShortDatePattern`, `PeriodFilterHelper` boundaries
  (first/last day of month, year rollover), `ObservableCollectionHelper` replace-in-place.
- **Navigation**: `NavTree.Categories` ids/labels/`ViewKey`s and the agreement with the React
  `NAV_TREE` (parity rule — same ids in both apps).
- Negative: every converter's non-matching input returns the documented fallback, never throws.

## Layer assignment

- **Unit only** — pure functions; converters are instantiated directly and called with
  `CultureInfo.InvariantCulture`. `Financial.Presentation.Tests` targets `net10.0-windows` with
  `UseWPF` so `Brush`/`Visibility` types resolve without a running application. No Integration,
  no E2E.

## Setup pattern

```csharp
using System.Globalization;
using Financial.Presentation.App.Converters;
using Financial.Presentation.App.Helpers;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DateFormatConverterTests
{
    private readonly DateFormatConverter _converter = new();

    [Fact]
    public void Convert_WithExplicitFormatParameter_FormatsUsingThatFormat()
    {
        var date = new DateTime(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        result.Should().Be("2026-07-05");
    }
}
```

(Verbatim from `Tests/Financial.Presentation.Tests/Converters/DateFormatConverterTests.cs`.)
`DateFormatConverterTests` is one of the files `docs/rules/implementation.md` §Tests names as
deliberately left without a shared initializer.

## When to skip

- `BindingProxy` (a `Freezable` holder), `BoolToSidebarWidthConverter` if it is a two-value
  lookup with no branch beyond true/false — one Theory covers it.
- `TreeViewDragDropBehavior` — attached-behavior wiring against real WPF events; covered
  manually, its decision logic belongs in the ViewModel (`MainNavigationViewModelMoveTests`).

## Examples from project

- `Tests/Financial.Presentation.Tests/Converters/*ConverterTests.cs` (15 files) — Unit.
- `Tests/Financial.Presentation.Tests/Behaviors/SortCycleTests.cs`, `NullLastComparerTests.cs` — Unit.
- `Tests/Financial.Presentation.Tests/Input/DecimalInputHelperTests.cs` — Unit.
- `Tests/Financial.Presentation.Tests/Helpers/PeriodFilterHelperTests.cs`, `DateFormatHelperTests.cs` — Unit.
- `Tests/Financial.Presentation.Tests/Navigation/NavTreeTests.cs` — Unit; four categories, settings/appearance child.
