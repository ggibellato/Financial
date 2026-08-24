# Implementation Plan: F03. Income Form Split Control

**Prerequisites:**
- .NET SDK / existing `Financial.slnx` build toolchain
- Node/npm for `Financial.Web`
- No new libraries, no new environment variables, no API/data changes (F01/F02 already shipped the fields this feature consumes)

### Stage 1: React Income Form Split Control

**1. Income Form Hook State** - Add the split-to-reserve field to the income form's create/edit state, defaulting from the selected income source's eligibility and recomputing whenever the selected source changes; wire it into the create/update payloads in place of the temporary hardcoded value; add a post-save confirmation message that appears when the saved income comes back split, and clears itself automatically after a short delay.

**2. Income Form and List Wiring** - Add the "Split to reserve" checkbox to the Income form, visible only for an eligible source; thread the new field and the confirmation message through the Monthly page's field-mapping and prop wiring; render the confirmation as a brief success banner near the income list.

### Stage 2: WPF Income Form Split Control

**3. Monthly View Model Split State** - Mirror the React hook's behavior in the view model: a bound split-to-reserve property, a computed eligibility-visibility property recomputed on source change, population on create/edit, inclusion in the save request, and the same post-save confirmation message with an auto-clearing delay.

**4. Income Form and List View Wiring** - Add the equivalent checkbox to the Income form view, bound to the view model's new properties; add the equivalent confirmation display near the income list view.

### Stage 3: Testing

**5. React Tests** - Cover the hook's default/recompute/edit-population/submit-payload/confirmation-timeout behavior, and the form/list components' conditional rendering of the checkbox and the confirmation banner.

**6. WPF Tests** - Cover the view model's equivalent computed-property, source-change-recompute, form-population, save-request, and confirmation-message-timing behavior.
