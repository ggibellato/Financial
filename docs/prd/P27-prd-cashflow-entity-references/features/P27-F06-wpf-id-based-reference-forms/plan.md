# Implementation Plan: F06. WPF Id-Based Reference Forms

**Prerequisites:**
- .NET SDK matching the existing `Financial.App`/`Financial.Presentation.Tests` projects
- No new NuGet packages or environment variables
- F05 merged (Web API routes and DTOs already Guid-based)

### Stage 1: ViewModel Property and Supporting-Type Changes

**1. Form Property Types** - Change `ExpenseFormPaymentSource`, `IncomeFormSource`, `IncomeFormBank`, `TransferFormSourceBank`, `TransferFormDestinationBank`, and `AdjustmentFormBankName` from `string` to `Guid?` on `MonthlyViewModel`, updating every read/write site (form defaults on create, population from a response DTO's `Id` field on edit, and every place a value is compared or checked for presence).

**2. Supporting Collections and Computed Properties** - Change `IncomeSourceOptions` to project `IncomeSourceDTO` objects instead of names; change `MarkPaidSources` to `Dictionary<Guid, Guid>` with a matching `SetMarkPaidSource(Guid, Guid)` signature; add `BankTotalRow.BankId` and use it to fix `AdjustmentFormBankName`'s current-balance lookup; add a new `AdjustmentFormBankDisplayName` computed property that resolves the selected Id back to a name for display; update `IsSameBankTransfer`, `IsAdjustmentBankSelected`, `ShowIncomeGrossValueField`, and `ShowRoundUpField` to compare/resolve by Id; change `ShowMoveMoneyFormCommand`/`ShowCreateTransferForm`'s parameter to `Guid?`.

**3. Shim Removal** - Delete `ResolveBankId` and `ResolveIncomeSourceId`, updating every one of their 12 call sites to pass the now-Guid form property directly into the Create/Update DTO.

### Stage 2: Validation and XAML

**4. Form Validators** - Change `ExpenseFormValidation`, `IncomeFormValidation`, and `TransferFormValidation`'s bank/source parameters from `string` to `Guid?`, updating their presence checks accordingly. Leave `BalanceAdjustmentFormValidation` unchanged (it never took a bank parameter).

**5. ComboBox Bindings** - Update the Income, Expense, Transfer, and Balance Adjustment form views' bank/source ComboBoxes from `SelectedValuePath="Name"` to `SelectedValuePath="Id"` (adding `DisplayMemberPath`/`SelectedValuePath` to the Income form's source ComboBox, which currently has neither); switch the Balance Adjustment form's current-balance display text to the new `AdjustmentFormBankDisplayName` property instead of the now-Guid `AdjustmentFormBankName`.

**6. Card Statement Pay-From Handler** - Update `CardsGridView.xaml.cs`'s selection-changed handler to pass the selected `BankDTO`'s `Id` instead of its `Name` into `SetMarkPaidSource`.

### Stage 3: Test Coverage

**7. ViewModel and Validator Test Updates** - Update every test in `MonthlyViewModelTests.cs`, `MonthlyViewModelBanksCardsTests.cs`, `ExpenseFormValidationTests.cs`, `IncomeFormValidationTests.cs`, and `TransferFormValidationTests.cs` that currently sets a form property or validator argument to a bank/source name string, switching it to the corresponding seeded entity's `Id` (or `Guid.NewGuid()`/`null` for negative cases), and confirm every assertion on a submitted Create/Update DTO's Guid fields still passes.

**8. Regression Confirmation** - Re-run `MonthlyViewModelBankOperationsTests.cs` and `BalanceAdjustmentFormValidationTests.cs` unchanged to confirm the explicitly out-of-scope bank filter and the untouched adjustment validator still behave identically, and confirm the full `Financial.Presentation.Tests` suite is green.
