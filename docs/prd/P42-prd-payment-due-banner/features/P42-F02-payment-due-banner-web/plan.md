# Implementation Plan: F02. Payment Due Banner (Web)

**Prerequisites:**
- F01 merged (`GET /api/v1/financial/payments-due` live, `PaymentDueDTO` present in `Financial.Web/src/api/generated/openapi.ts`)
- No new npm packages — `@fluentui/react-components` and `@fluentui/react-icons` are already dependencies

### Stage 1: API Client

**1. Payment Due DTO Type and Client Method** - Add the `PaymentDueDto` type alias and a `getPaymentsDue()` method to the API client, following the existing simple-GET pattern used for `getSyncStatus`.

### Stage 2: Fetch and Lifecycle Hook

**2. usePaymentsDue Hook** - Add the hook that fetches the payments-due list once on mount, holds it in state only while non-empty, and manages the 10-second auto-dismiss timer plus a manual dismiss function, failing silently on an empty response or a fetch error. Reference the spec for the exact state shape and timer behavior.

**3. Hook Tests** - Write the test suite covering fetch-once behavior, the empty/error fail-safe paths, the auto-dismiss timer, and manual dismiss, per the spec's testing strategy.

### Stage 3: Banner Component and Mounting

**4. PaymentDueBanner Component** - Add the component that renders nothing when there are no payments, and otherwise renders the banner title, one row per payment with its urgency icon/color and details, and a close button. Reference the spec for the urgency-tier mapping and accessibility requirements.

**5. Mount in App Shell** - Add `<PaymentDueBanner>` to `App.tsx` next to the existing `<SyncStatusBanner>`.

**6. Component Tests** - Write the test suite covering render/no-render states, per-item content and urgency labeling, item ordering, and close-button behavior (click and keyboard), per the spec's testing strategy.
