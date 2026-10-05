# Implementation Plan: WPF FlaUI E2E Suite

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, a Windows machine with an interactive desktop for the local runs
- Stages merge in order: 2 needs the ids from 1, 3 needs the project from 2
- Drive the app only with the fixture's temp data; never against the live data files or Google Drive (`feedback_never_run_migrations_against_live_data`)
- Locate WPF controls by AutomationId, never by screenshot coordinates (`feedback_wpf_automation_clickable_point`)

### Stage 1: AutomationIds and Their Contract (PR1)

**1. Shell and navigation ids** - Add the main window, breadcrumb, sidebar and per-item navigation ids, the latter bound to the navigation item id so each templated button is unique.

**2. Investment ids** - Add the investment tree and asset summary ids in the navigation view.

**3. Monthly and expense form ids** - Add the monthly tabs, error, retry, category grid and total, expense grid, new-expense button, banks grid, and every field, error and button of the expense form.

**4. Contract test** - Add the Presentation test that asserts each required id exists once in its XAML file and that every id in the project matches the kebab-case convention.

**5. Convention document** - Document the naming rule, the id table and how to add one in the WPF UI guide, and note it in the project's CLAUDE.md.

**6. UI review** - Check the touched views against the UI review checklist; the change adds no visible behaviour, so the record is "no visual or interaction change".

### Stage 2: E2E Project, Launch Fixture, CI Job (PR2)

**7. Project and fixture** - Add `Financial.App.E2ETests` to the solution with FlaUI, the per-test session (temp data, environment overrides, window wait, teardown), the live-data guard, orphan reaper, collection serialisation and lookup helpers.

**8. First journey** - Add the app-starts journey and run it locally to prove the fixture end to end, including that no process or temp directory is left behind.

**9. Failure artifacts** - Capture a screenshot, the app log and the exit code when a test fails, and prove it with a deliberately failing local run.

**10. CI wiring** - Add the `wpf-e2e` job, put it in `ci-status` needs, exclude the project from the backend run, add the classifier rule and its self-test cases, and upload artifacts on failure.

**11. Documentation** - Update the pipeline document with the job, trigger, artifacts and the nightly fallback rule.

### Stage 3: Remaining Journeys, Docs and PRD Ticks (PR3)

**12. Journeys** - Add the asset, add-expense, blank-value and monthly-summary journeys, locating by id and by visible names only where the label is the contract.

**13. CI proof** - Push one deliberately failing commit to show the artifacts upload from `windows-latest`, record the job duration against the 8-minute budget, then revert it.

**14. Guides** - Update the testing guide (WPF E2E now exists, how to run it, locator rule) and the CLAUDE.md command lines.

**15. PRD amendments and ticks** - Amend the F13 blank-value wording, then tick the satisfied boxes in a separate commit.
