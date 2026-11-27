# Implementation Plan: Brazilian CNPJ Identifiers

**Prerequisites:**
- .NET 10 SDK and Node with `Financial.Web` dependencies installed (`npm ci`).
- No new packages, environment variables or configuration.
- Work on a branch off `origin/main`. Use a worktree only if another session is using the repo folder. In wave 1 this feature's PR 1 merges before F01's PR 2, which then rebases and regenerates the OpenAPI files.
- Read `spec.md` in this folder. Every rule, name and message referenced below is defined there.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`).

### Stage 1: Backend contract (PR 1)

**1. CNPJ domain rule** - Add the CNPJ normalisation and check-digit rule to the Investment Domain rules, with the single invalid-value message the PRD defines.

**2. Asset fields** - Give the asset its optional CNPJ and Administrator CNPJ identity fields, the rules for which assets carry each (every BR asset; BR funds, FIIs and ETFs), and setters that validate and apply those rules. Move the identity update's segment clearing into one helper that also clears each CNPJ when the asset stops qualifying.

**3. Asset admin and details DTOs** - Add both fields to the asset admin read, create and update DTOs and to the asset details DTO.

**4. Service wiring** - Have the asset admin service validate both values before the save, next to the existing segment normalisation, then set them on create and update, and map them to the admin DTO. Map them in the asset details read as well.

**5. Contract regeneration** - Regenerate the OpenAPI snapshot with the contract test filter, review the diff (only the new `cnpj` and `administratorCnpj` properties), then regenerate the web API types and commit both.

### Stage 2: Web form and Summary tab (PR 2)

**6. CNPJ web helpers** - Add one web utility holding the client validity check and message, digit stripping, masking and which assets carry each field, and give the shared form hook a way to reveal one field's error on blur.

**7. Asset form fields** - Add the CNPJ field under ISIN for BR assets and the Administrator CNPJ field after it for BR funds, FIIs and ETFs, through one shared local field helper, with the PRD helper texts, on-blur and on-Save validation per field, stored values shown masked, and the digits in the submitted values.

**8. Assets page wiring** - Send both fields on asset create and update, or null when cleared or hidden.

**9. Summary tab** - Show each masked value, or "None", after ISIN in the asset Summary identity group where its field applies.

**10. PRD and docs** - In the last commit of PR 2, give the PRD's F06 checkboxes their ids and tick them. Record any deviation from this spec or plan in the same docs commit and the PR body.
