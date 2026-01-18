# CI/CD plan (GitHub Actions) — Build & Test only

## Goals
- Provide fast, reliable CI for the .NET 9 Blazor solution.
- Run on:
  - Pull requests (validation)
  - Pushes to `master`
- Enforce quality gates:
  - Build succeeds with **zero warnings**
  - All tests pass
- No deployments (MVP). Azure deployments remain manual from Visual Studio.

## Current state
- A GitHub Actions workflow exists: `.github/workflows/build-test.yml` (to be aligned with this plan).

## Proposed CI workflows
### 1) `build-test.yml` (PR + push)
**Triggers**
- `pull_request` targeting `master`
- `push` to `master`

**Jobs**
1. `build_and_test`
   - Checkout
   - Setup .NET SDK `9.x`
   - Restore
   - Build (treat warnings as errors)
   - Test

**Quality gate implementation**
- Enforce “no warnings” using:
  - `dotnet build -warnaserror` (preferred for CI)

**Test command**
- `dotnet test --configuration Release --no-build`

## Branch policy recommendations (GitHub settings)
- Protect `master`:
  - Require status checks to pass before merging
  - Require PR (no direct pushes)
  - Require the GitHub Actions workflow check(s)

## Implementation steps
1. Update `.github/workflows/build-test.yml`:
   - Ensure triggers: `pull_request` + `push` for `master`
   - Ensure .NET 9 SDK setup
   - Add build with warnings-as-errors
   - Run unit tests for `PromptHub.Web.UnitTests`
2. Validate by pushing a branch and opening a PR.

## Acceptance criteria
- Opening a PR to `master` runs GitHub Actions and:
  - `dotnet build` completes with zero warnings
  - `dotnet test` passes
- Pushing to `master` runs the same checks.
- No deployment steps exist in CI.
