# Implementation Plan: WPF Dynamic Picklist

**Prerequisites:**
- F02 (Category entity-reference wire contract) and F03 (`ICategoryService.GetCategories()`) merged to `main`
- No new NuGet packages

### Stage 1: ViewModel Category Fetch

**1. Category Collection in MonthlyViewModel** - Replace the hardcoded static category list with an `ObservableCollection<CategoryDTO>` fetched inside the existing `RefreshAsync`, alongside banks/income sources, and an active-only filtered view for the expense-entry picker.

### Stage 2: Expense Form Rewire

**2. Category Field and Validation** - Rename the expense form's category field from a name string to a nullable Id, update validation to check for a missing Id, and update the create/edit-form population and save logic to use the Id directly instead of resolving a name at save time.

**3. Category ComboBox Binding** - Point the expense form's Category dropdown at the live active-category collection, binding by Id the same way the Card dropdown already does.

### Stage 3: Tests

**4. ViewModel and Validation Tests** - Add coverage for the new category collection and active filtering, and update existing expense create/edit and validation tests for the Id-based contract.
