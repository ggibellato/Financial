# Implementation Plan: Fund CNPJ Identifier

**Prerequisites:**
- .NET 10 SDK and Node with `Financial.Web` dependencies installed (`npm ci`).
- No new packages, environment variables or configuration.
- Work on a branch off `origin/main`. Use a worktree only if another session is using the repo folder. In wave 1 this feature's PR 1 merges before F01's PR 2, which then rebases and regenerates the OpenAPI files.
- Read `spec.md` in this folder. Every rule, name and message referenced below is defined there.

### Stage 1: Backend contract (PR 1)

**1. CNPJ domain rule** - Add the CNPJ normalisation and check-digit rule to the Investment Domain rules, with the single invalid-value message the PRD defines. No comments or XML docs.

**2. Asset field** - Give the asset its optional CNPJ identity field, the rule for which assets carry it, and a setter that validates and applies that rule. Move the identity update's segment clearing into one helper that also clears the CNPJ when the asset stops being a Brazilian fund.

**3. Asset admin and details DTOs** - Add the CNPJ to the asset admin read, create and update DTOs and to the asset details DTO.

**4. Service wiring** - Have the asset admin service set the CNPJ on create and update inside the save (the setter validates), and map it to the admin DTO. Map it in the asset details read as well.

**5. Contract regeneration** - Regenerate the OpenAPI snapshot with the contract test filter, review the diff (only the four new `cnpj` properties), then regenerate the web API types and commit both.

### Stage 2: Web form and Summary tab (PR 2)

**6. CNPJ web helpers** - Add the client validity check and message next to the existing ISIN validator, and a small utility for digit stripping, masking and which assets carry a CNPJ.

**7. Asset form field** - Add the CNPJ field under ISIN in the asset dialog for Brazilian funds only, with the PRD helper text, on-blur and on-Save validation, the stored value shown masked, and the CNPJ digits in the submitted values.

**8. Assets page wiring** - Send the CNPJ on asset create and update, or null when it is cleared or hidden.

**9. Summary tab** - Show the masked CNPJ, or "None", after ISIN in the asset Summary identity group for Brazilian funds.

**10. PRD and docs** - In the last commit of PR 2, give the PRD's F06 checkboxes their ids and tick them. Record any deviation from this spec or plan in the same docs commit and the PR body.
