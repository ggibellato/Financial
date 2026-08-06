# Implementation Plan: WPF Income Form Dynamic Source Picklist

**Prerequisites:**
- F04 merged (`IIncomeSourceService`/`IncomeSourceDTO` already exist in `Financial.CashFlow.Application`, already registered in DI)
- No new NuGet packages required

### Stage 1: ViewModel and DI Wiring

**1. Inject and Fetch Income Sources** - Inject `IIncomeSourceService` into `MonthlyViewModel` and fetch the income source list during the existing period-load, alongside banks. Wire the new dependency through the app's dependency injection composition root.

### Stage 2: Dynamic Picklist and Default Selection

**2. Active, Ordered Picklist** - Replace the hardcoded source list backing the form's dropdown with the fetched list, filtered to active sources and ordered to match the current display order. Default the "new income" form's selected source from this list instead of a hardcoded name, mirroring how the default bank is already selected.
