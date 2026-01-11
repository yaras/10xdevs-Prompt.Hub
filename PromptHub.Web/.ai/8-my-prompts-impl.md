# My Prompts page (Blazor + MudBlazor) - implementation plan

## Goal
Create a new Blazor page under `PromptHub.Web/Components/Pages` named `MyPrompts.razor` that:
- displays the current user's prompts in a responsive grid using MudBlazor.
- loads prompts asynchronously.
- provides a floating action button (FAB) to open a dialog to create a prompt.
- opens an edit dialog when an existing prompt is clicked.

## Assumptions / dependencies
- MudBlazor is (or will be) referenced and configured in `PromptHub.Web`.
- Auth is enabled and user is authenticated (fallback policy already requires auth).
- Storage layer already exposes:
  - `IPromptReadStore.ListMyPromptsAsync(authorId, token, pageSize, ct)`
  - `IPromptWriteStore.CreateAsync(prompt, ct)`
  - `IPromptWriteStore.UpdateAsync(prompt, expectedETag, ct)`
  - `IPromptReadStore.GetByIdForAuthorAsync(authorId, promptId, ct)`

If MudBlazor is not yet configured, add:
- `builder.Services.AddMudServices()` in `Program.cs`
- Mud providers in layout (e.g., `MudThemeProvider`, `MudDialogProvider`, `MudSnackbarProvider`).

## UI/UX outline
### Page layout
- Header row: title “My prompts”
- Grid of prompt cards:
  - Use `MudGrid` + `MudItem` and inside each a `MudCard`.
  - Card shows title, tags (chips), visibility, last updated, likes/dislikes.
- Card shows author email (when available).
  - Entire card clickable to open edit dialog.
- Loading states:
  - While loading: `MudProgressCircular` centered.
  - Empty state: `MudText` + CTA to create first prompt.
- Floating action button:
  - `MudFab` anchored bottom-right.
  - On click: open create dialog.

### Dialogs
Create a single reusable dialog component:
- `PromptHub.Web/Components/Dialogs/PromptEditorDialog.razor`
- Used for both create and edit.

Dialog behavior:
- Parameters:
  - `Mode` (Create/Edit)
  - `Model` (`PromptModel` or a lightweight editable DTO)
- Fields:
  - Title (required)
  - Content/body (required)
  - Tags (chip input)
  - Visibility (enum select)
- Buttons:
  - Cancel
  - Save (Create/Update)

Validation:
- Use `MudForm` with required validators.

Concurrency:
- For Edit, keep original `ETag` from the loaded `PromptModel` and pass it to `UpdateAsync`.
- Show a friendly message on ETag conflict (reload required).

## Data loading & state management
### AuthorId resolution
- Use `AuthenticationStateProvider` and extract Entra `oid` claim.
- Fail gracefully if missing (show message).

### AuthorEmail resolution
- Capture email from identity claims when available (e.g., `email`, `preferred_username`, `upn`) and persist it with the prompt so lists can display it without extra lookups.

### Loading prompts
- In `MyPrompts.razor.cs`, in `OnInitializedAsync`:
  1. Resolve `authorId`
  2. Call `ListMyPromptsAsync(authorId, token: null, pageSize: 50, ct)`
  3. Store results in local list
- Optional pagination:
  - Keep `ContinuationToken` returned in `ContinuationPage<T>`.
  - Add “Load more” button at bottom, or implement infinite scroll later.

### Refresh after create/edit
- After dialog returns success:
  - Either reload the list from scratch (simplest)
  - Or update local list in-place (optimization)

## Implementation steps (files)
1. **Create page**
   - Add `PromptHub.Web/Components/Pages/MyPrompts.razor`
     - `@page "/my-prompts"`
     - Uses MudBlazor grid + FAB.
   - Add code-behind `PromptHub.Web/Components/Pages/MyPrompts.razor.cs`
     - Inject `IPromptReadStore`, `IPromptWriteStore`, `IDialogService`, `AuthenticationStateProvider`.
     - Load prompts asynchronously.

2. **Create dialog**
   - Add `PromptHub.Web/Components/Dialogs/PromptEditorDialog.razor`
   - Add code-behind `PromptHub.Web/Components/Dialogs/PromptEditorDialog.razor.cs`
   - Use MudBlazor form controls and return a result indicating create/update.

3. **Wire up MudBlazor (if not already)**
   - Update `PromptHub.Web/Program.cs` to register Mud services.
   - Update `MainLayout` or `App` to include Mud providers.

4. **Navigation**
   - Update `PromptHub.Web/Components/Layout/NavMenu.razor` to add a link to `/my-prompts`.

5. **Testing (optional but recommended)**
   - Add bUnit tests in `PromptHub.Web.UnitTests`:
     - renders loading state
     - renders list state
     - FAB opens dialog
     - clicking card opens edit dialog

## Acceptance criteria
- Navigating to `/my-prompts` shows a grid of the signed-in user's prompts.
- Prompts are loaded asynchronously with a visible loading indicator.
- FAB opens a dialog to create a prompt; successfully saving refreshes the list.
- Clicking a prompt opens a dialog to edit it; successfully saving refreshes the list.
- No Razor styling violations:
  - one root-level component per `.razor` file
  - public parameters documented
  - logic in `.razor.cs` for non-trivial code

## Notes / open questions
- Exact `PromptModel` shape (fields for content, ETag, etc.) should be confirmed before implementing dialog binding.
- Decide whether tags edit uses free-text chips or a controlled catalog from `ITagCatalogStore`.
