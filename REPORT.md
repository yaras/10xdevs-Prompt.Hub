# PromptHub — 10xDevs Certification Project Analysis

Date: 2026-01-18

This report audits the project in this repository against the 10xDevs certification-style criteria.

---

## 1) Checklist

1. **Documentation (README + PRD)** ✅
   - Found a meaningful root README at `README.md` describing the app, stack, local run steps, and scripts.
   - Found a PRD in `.ai` at `PromptHub.Web/.ai/1-prd.md` with detailed requirements (auth gate, CRUD, voting rules, AI tag suggestions, etc.).

2. **Login functionality** ✅
   - Authentication is implemented via Microsoft Identity / Entra ID.
   - UI wiring is present (e.g., `PromptHub.Web/Components/Layout/LoginDisplay.razor` links to `MicrosoftIdentity/Account/SignIn` and `.../SignOut`).
   - App-level auth is configured in `PromptHub.Web/Program.cs` (Microsoft.Identity.Web + authorization policy).

3. **Test presence** ✅
   - Dedicated unit test project exists: `PromptHub.Web.UnitTests/`.
   - Meaningful tests found:
     - `PromptHub.Web.UnitTests/Votes/PromptVotingFeatureTests.cs` (vote transitions, validation, aggregate delta logic)
     - `PromptHub.Web.UnitTests/OpenAI/OpenAiTagSuggestionServiceTests.cs` (input validation + allowed-tags behavior)

4. **Data management** ✅
   - Data persistence is implemented using Azure Table Storage.
   - Storage infrastructure exists under `PromptHub.Web/Infrastructure/TableStorage/` (tables, entities, mapping, stores).
   - The application wires persistence via DI in `PromptHub.Web/Infrastructure/DI/TableStorageServiceCollectionExtensions.cs`.

5. **Business logic** ✅
   - Non-trivial app logic exists beyond basic CRUD:
     - Voting business rules (toggle behavior, like/dislike state transitions, aggregate delta computation) in the votes feature (and tested).
     - AI tag suggestion constraints/validation (allowed-tag filtering, max suggestions, schema-validated JSON) in `PromptHub.Web/Infrastructure/OpenAI/OpenAiTagSuggestionService.cs`.

6. **CI/CD configuration** ✅
   - GitHub Actions workflow found in hidden CI/CD directory: `.github/workflows/build-test.yml`.
   - Workflow runs `dotnet restore`, `dotnet build`, and `dotnet test` on pushes/PRs.

---

## 2) Project Status

- **Score:** 6/6
- **Status:** **100%**

---

## 3) Priority Improvements (Actionable)

Nothing is missing for the six required criteria. If you want to strengthen “submission polish” and reduce reviewer friction, these are high-impact improvements:

1. **Add a conventional `/docs` index (optional)**
   - Create `docs/README.md` and link to the PRD (`PromptHub.Web/.ai/1-prd.md`) plus architecture notes.
   - Rationale: some reviewers look specifically for `/docs` even if `.ai/` exists.

2. **Clarify required configuration keys + examples**
   - Expand `README.md` with an explicit table of required settings (Storage, Entra, OpenAI) and sample `appsettings.Development.json` values (non-secrets).
   - Ensure it matches `PromptHub.Web/appsettings.json` and the scripts under `scripts/`.

3. **CI/CD: add artifact/publish step (optional)**
   - Extend `.github/workflows/build-test.yml` to publish the web project (`dotnet publish`) and upload artifacts.
   - Rationale: makes the pipeline closer to a “deployable build” even if you don’t deploy in CI.

---

## 4) Summary for Submission Form (2–3 sentences)

PromptHub is a Blazor Server app (with MudBlazor) that lets authenticated users store and share AI prompts, vote on prompts, and get AI-assisted tag suggestions. It includes Microsoft Entra ID login via Microsoft.Identity.Web, persists data in Azure Table Storage, and contains meaningful unit tests for voting and tag suggestion logic. CI is set up with GitHub Actions to restore, build, and test on every push/PR.
