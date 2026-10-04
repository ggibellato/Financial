Implement and standardize end-to-end UI testing for both frontend applications using the approach below.

# Objective

Use the appropriate UI automation framework for each frontend:

- Use **Playwright** for the React web application.
- Use **FlaUI with UIA3** for the native WPF desktop application.
- Do not attempt to use Playwright to automate native WPF controls.
- Do not duplicate shared business-flow coverage across both UI suites when the same behavior can be validated below the UI layer.

# Test strategy

Follow this test responsibility split:

| Test level | Responsibility | Primary technologies |
|---|---|---|
| Unit | Business rules, calculations, validation, error handling, and edge cases in isolation | xUnit/NUnit, Vitest, mocks only when required |
| Component/feature | A feature’s acceptance behavior using real internal application components | .NET test framework, React Testing Library/Vitest |
| Integration | API, database, messaging, and internal service interaction within the domain | .NET test framework, Testcontainers or dedicated test infrastructure |
| UI E2E | A limited set of critical user journeys specific to each frontend | Playwright for React; FlaUI UIA3 for WPF |

Use component, integration, and API tests as the main protection for shared business behavior. Use UI E2E tests only to validate that each frontend correctly exposes and completes its critical user journeys.

Do not create the same complete business-flow test in both the React and WPF E2E suites unless there is a frontend-specific reason to do so. If a flow is shared, test its business rules and API behavior below the UI layer, then keep one or two UI smoke tests per frontend to confirm that the frontend can complete the flow.

# React web E2E tests

Keep Playwright as the E2E test framework for the React application.

Configure Playwright to run headlessly by default in CI. The browser must not visibly open during normal automated test runs.

Use this baseline configuration pattern and adapt paths, URLs, and commands to the repository:

```ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,

  use: {
    baseURL: process.env.WEB_BASE_URL ?? 'http://localhost:5173',
    headless: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
});
```

Apply these rules to Playwright tests:

- Use accessible locators first, including `getByRole`, `getByLabel`, and `getByText` only where the visible text is intentionally part of the UI contract.
- Use stable `data-testid` values only where an accessible locator is not practical or would be unstable.
- Do not locate elements using CSS classes, generated IDs, DOM position, XPath, or timing assumptions.
- Do not use arbitrary delays such as `waitForTimeout` except where there is a documented and unavoidable external constraint.
- Wait for meaningful application conditions, visible outcomes, navigation completion, API responses, or stable UI state.
- Capture traces, screenshots, and videos only on failure or retry.
- Ensure tests are isolated and do not depend on execution order.
- Use unique or resettable test data for every test run.
- Run Playwright E2E tests in parallel only where the test data and backend state are isolated.

Support headed execution only for local debugging:

```bash
npx playwright test --headed
npx playwright test --ui
npx playwright test --debug
```

Normal CI execution must use:

```bash
npx playwright test
```

# WPF E2E tests

Create a dedicated WPF UI automation test project using:

- `FlaUI.Core`
- `FlaUI.UIA3`
- The repository’s existing .NET test framework, preferably xUnit if it is already used

Use UIA3 as the default automation backend for WPF. Do not introduce an older desktop UI automation framework unless there is a documented compatibility reason.

The WPF E2E test project must:

- Launch the WPF application as a real desktop process.
- Attach to the application using FlaUI UIA3.
- Interact only through Windows UI Automation controls.
- Assert user-visible outcomes and stable application state.
- Close the application process after every test or test collection.
- Clean up generated test data and temporary files.
- Prevent orphaned application processes after failures.
- Run tests sequentially unless separate isolated Windows desktop sessions are available.

Add stable UI Automation identifiers to WPF controls that participate in E2E workflows.

Use `AutomationProperties.AutomationId` in XAML:

```xml
<TextBox
    x:Name="EmailTextBox"
    AutomationProperties.AutomationId="login-email" />

<PasswordBox
    x:Name="PasswordTextBox"
    AutomationProperties.AutomationId="login-password" />

<Button
    Content="Sign in"
    AutomationProperties.AutomationId="login-submit" />

<Grid
    AutomationProperties.AutomationId="dashboard-screen">
    <!-- Dashboard content -->
</Grid>
```

Follow these WPF test rules:

- Locate controls by `AutomationId` wherever possible.
- Use accessible names only where the displayed label is intentionally part of the user-facing contract.
- Do not locate elements by screen coordinates, visual-tree position, generated IDs, or fragile window titles.
- Do not use `Thread.Sleep` as synchronization.
- Wait for a meaningful UI Automation condition, such as a control becoming available, enabled, visible, or changing state.
- Give every test-owned window, dialog, button, input, and important result view a stable automation identifier.
- Keep WPF automation IDs stable across refactors unless the user-facing feature contract has changed.
- Add a clear comment or documentation convention for the naming of automation IDs.
- Keep all automation IDs unique within a screen or dialog.

Use a test structure similar to this, adapting namespaces, executable paths, and assertion helpers to the existing solution:

```csharp
using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;

public sealed class LoginWindowE2ETests : IDisposable
{
    private readonly Application _application;
    private readonly UIA3Automation _automation;

    public LoginWindowE2ETests()
    {
        _automation = new UIA3Automation();

        _application = Application.Launch(
            @"C:\build\artifacts\MyApplication.Wpf.exe");
    }

    [Fact]
    public void User_can_sign_in_with_valid_credentials()
    {
        var window = _application.GetMainWindow(_automation);

        var email = window
            .FindFirstDescendant(cf => cf.ByAutomationId("login-email"))
            .AsTextBox();

        var password = window
            .FindFirstDescendant(cf => cf.ByAutomationId("login-password"))
            .AsTextBox();

        var submitButton = window
            .FindFirstDescendant(cf => cf.ByAutomationId("login-submit"))
            .AsButton();

        email.Enter("e2e.user@example.test");
        password.Enter("test-password-from-secure-configuration");
        submitButton.Invoke();

        var dashboard = window.FindFirstDescendant(
            cf => cf.ByAutomationId("dashboard-screen"));

        Assert.NotNull(dashboard);
    }

    public void Dispose()
    {
        try
        {
            _application.Close();
        }
        finally
        {
            _automation.Dispose();
        }
    }
}
```

Do not hard-code production-style credentials in test source code. Read test credentials and environment-specific configuration from secure CI variables or local development secrets.

# Authentication and test data

Treat authentication and test data as security-sensitive test infrastructure.

Apply the following rules:

- Never use production users, production credentials, or production data.
- Never commit passwords, client secrets, tokens, connection strings, or MFA bypass values to source control.
- Use dedicated non-production test users with minimum required permissions.
- Use a dedicated non-production identity-provider tenant, realm, or test configuration where possible.
- Store secrets in the CI secret manager and local developer secret storage.
- Use short-lived credentials where supported.
- Ensure each test run starts from known test data or uses unique generated test data.
- Ensure each test cleans up data it created, or provide a reliable environment reset mechanism.
- Do not make all pull-request tests depend on a live external identity provider unless this is an explicit and accepted requirement.
- If real external authentication must be tested, keep it in a small scheduled or pre-release smoke suite rather than running it for every pull request.

# CI requirements

Create separate test execution stages.

```text
Pull request pipeline
├── Unit tests
├── Component or feature tests
├── Integration tests
├── React Playwright smoke E2E tests
└── WPF FlaUI smoke E2E tests on a Windows runner

Nightly or pre-release pipeline
├── Full unit, component, and integration suites
├── Full React Playwright E2E suite
├── Full WPF FlaUI E2E suite
└── Small real external-authentication smoke suite, when applicable
```

Apply these CI rules:

- Run React Playwright tests on a suitable browser-capable runner.
- Run WPF FlaUI tests only on Windows.
- Run WPF FlaUI tests in an interactive desktop environment capable of showing and automating real windows.
- Do not assume WPF UI automation will work reliably on a locked machine, disconnected desktop session, generic headless Linux runner, or ordinary server environment.
- Use a dedicated Windows runner or VM for WPF UI automation where possible.
- Do not run multiple WPF UI tests in parallel in the same Windows desktop session.
- Publish Playwright traces, screenshots, videos, and test reports when tests fail.
- Publish WPF test logs, screenshots where supported, application logs, and crash information when tests fail.
- Ensure failed tests terminate all application processes they started.

# Test scope

Initially implement only the highest-value smoke tests.

For the React application, cover:

- Application starts successfully.
- A user can authenticate using the approved non-production test mechanism.
- A user can navigate to the main dashboard or landing screen.
- A user can complete one critical business workflow.
- A failed or unauthorized action shows an appropriate user-facing result.

For the WPF application, cover:

- The application starts successfully.
- The login or approved test-authentication entry point is available.
- A user can complete the most important desktop workflow.
- Important validation and error states are visible and understandable.
- The main post-login or post-action screen is displayed successfully.

Do not add UI E2E tests merely to increase test count or line coverage. Every UI E2E test must protect a meaningful user workflow, a critical integration boundary, a security-sensitive action, or a regression that cannot be adequately detected below the UI layer.

# Acceptance criteria

The implementation is complete only when all of the following are true:

- Playwright remains the standard UI E2E framework for the React frontend.
- Playwright runs headlessly by default in CI.
- FlaUI with UIA3 is introduced as the standard UI E2E framework for the WPF frontend.
- WPF controls used by E2E tests expose stable `AutomationProperties.AutomationId` values.
- React tests use stable accessible locators or deliberate `data-testid` values.
- Neither suite relies on fixed timing delays, element coordinates, fragile selectors, or test execution order.
- Test credentials and secrets are not committed to the repository.
- Test data is isolated, deterministic, and cleaned up or reset reliably.
- React UI tests can run safely in parallel when supported by isolated data.
- WPF UI automation tests run sequentially on an interactive Windows runner.
- E2E artifacts are available after failures for diagnosis.
- The initial suite contains only meaningful critical smoke tests, not duplicated exhaustive UI coverage.