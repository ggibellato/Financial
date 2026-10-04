# Implementation Plan: CI Gate Hardening

**Prerequisites:**
- .NET 10 SDK, an up-to-date `main`, `gh` authenticated for the repository
- Each stage below is one PR; they are independent and may merge in either order

### Stage 1: Contract and Architecture Guards (PR1)

**1. Snapshot update guard** - Make the OpenAPI contract test fail with the documented message when the update flag is set on CI, keeping the local regeneration flow intact, and cover both paths with tests.

**2. Bounded-context isolation rule** - Add the architecture rule that fails when any CashFlow assembly references an Investment assembly, or the reverse, across Domain, Application and Infrastructure, with a message naming the offending reference.

**3. Domain purity rule** - Add the architecture rule that fails when either Domain assembly references ASP.NET Core, the JSON library or any Infrastructure assembly.

**4. Revert checks** - Prove each new guard fails on a deliberate violation, record the result in the PR description, and revert the violation.

### Stage 2: Classifier, Self-Test and Documentation (PR2)

**5. Classifier rules** - Reorder the Markdown rule so documents inside source directories follow their directory's rule, send front-end desktop changes to the backend job as well, and make the script's main body sourceable.

**6. Classifier self-test** - Add the fixture-driven self-test with at least fifteen path cases, run it as the first step of the `changes` job, and prove it fails when a rule is deliberately broken.

**7. Architecture tests once** - Remove the architecture test run from the `wpf` job so it runs in `backend` only.

**8. Pipeline documentation** - Update the rule and job tables in the pipeline document and add the merge-gate bypass section recording that administrator enforcement is deliberately off and that any administrator merge on a red build is followed by a fix PR.
