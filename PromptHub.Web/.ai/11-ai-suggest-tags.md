# AI suggest tags (Blazor + MudBlazor) – implementation plan

## Goal
Add an AI-powered “Suggest tags” capability to the **Add/Edit Prompt dialog** (`PromptEditorDialog`) that:
- takes the current **Title only** (per decision)
- calls an **OpenAI SDK** powered backend service
- returns a small set of suggested tags
- **adds them into the dialog’s selected tags list** as if the user added them manually
- shows errors as a **toast/snackbar** and logs details (no sensitive info shown)
- if a similar tag already exists, it is not duplicated

This work is explicitly limited to the dialog UX (no page-level integration).

## Decisions captured (from Q&A)
- Scope: dialog only.
- Allowed tags source: **configuration list** (appsettings) of potential tags.
- Provider: **OpenAI official SDK**; API key already available.
- Trigger: manual click of a `Suggest tags` button.
- Behavior: suggestions are applied into the tags list (dedupe + enforce max).
- Errors: show snackbar/toast + log entry.
- Prompt input: **Title only**.

## Non-goals (for this iteration)
- Auto-trigger on title blur.
- Using prompt content/body for suggestions.
- Admin UI to manage tag catalog.
- Persisting suggestions separately (only apply to selected tags).

---

## UX / UI requirements (MudBlazor)
### Dialog UI changes (`PromptEditorDialog`)
- Add a secondary button next to the primary save button:
  - Label: `Suggest tags`
  - Style: `Variant.Outlined` or `Color.Secondary`
  - Disabled when:
    - title is empty/whitespace
    - a suggestion request is in-flight
    - already at tag limit (10)
- While request is running:
  - show inline spinner in the button (e.g., `MudProgressCircular` small)
  - keep dialog usable (non-blocking), but avoid concurrent requests

### Applying suggestions to selected tags
On success:
- suggestions are normalized to lower-case
- discard tags not in the configured allowed-tags list
- deduplicate against current selected tags
- enforce max tags: 10
- apply by updating the same collection used by the “manual add” flow so validation and rendering behave identically

User feedback:
- show snackbar: `Added N suggested tags`

### Error handling
- On failure: show snackbar with a generic message (no stack trace), e.g.:
  - `Unable to suggest tags right now. Please try again.`
- Log the exception via `ILogger<T>` with structured context:
  - author id (if available)
  - prompt id (if edit)
  - title length (not full title, optional)

---

## Configuration
Add an options object in `PromptHub.Web`:
- `TagSuggestionOptions`
  - `AllowedTags: string[]` (lower-case canonical list)
  - `MaxSuggestions: int` default 4
  - `Model: string` (e.g., `gpt-4o-mini` or equivalent)

Add settings to `appsettings.json` (and environment variants as needed):
```json
{
  "TagSuggestion": {
    "AllowedTags": ["blazor", "azure", "testing"],
    "MaxSuggestions": 4,
    "Model": "gpt-4o-mini"
  }
}
```
Note: keep the Real allowed-tag list small enough for prompt-size, or generate a compact instruction string.

Secrets:
- store OpenAI API key in user secrets / environment variables (not committed)
  - e.g., `OpenAI:ApiKey`

---

## Service design
### Public interface
Create an abstraction in the Web layer (or a suitable inner “features/services” folder consistent with the solution):
- `ITagSuggestionService`
  - `Task<IReadOnlyList<string>> SuggestTagsAsync(string title, CancellationToken ct)`

### OpenAI implementation
Create `OpenAiTagSuggestionService` implementing `ITagSuggestionService`.
Responsibilities:
- validate input title (non-empty)
- build a prompt instructing:
  - only return tags from the provided allowed list
  - return up to `MaxSuggestions`
  - return machine-readable output (JSON) validated against a JSON Schema
- make the OpenAI SDK call
- validate output against the JSON Schema
- normalize to lower-case
- return list

Prompting strategy (recommended):
- System instruction: you are a tag suggestion engine
- User message includes:
  - title
  - allowed tags
  - constraints: return JSON array of strings, no prose

Parsing:
- use JSON Schema validation to enforce output contract and avoid ad-hoc parsing
- treat schema validation failures as errors (log + user-friendly snackbar in UI)

Resilience:
- set a reasonable timeout (e.g., 5–10 seconds)
- don’t retry aggressively (OpenAI SDK may have built in policies; otherwise one retry max)

Logging:
- log failures with `ILogger<OpenAiTagSuggestionService>`
- do not log API key or full prompt/response

---

## Dependency injection wiring
In `PromptHub.Web/Program.cs`:
- register options:
  - `builder.Services.Configure<TagSuggestionOptions>(builder.Configuration.GetSection("TagSuggestion"));`
- register OpenAI client + service:
  - configure OpenAI SDK client using `OpenAI:ApiKey`
  - `builder.Services.AddScoped<ITagSuggestionService, OpenAiTagSuggestionService>();`

Lifetime:
- service as `Scoped` (matches Blazor Server patterns)
- OpenAI client typically `Singleton` or `Scoped` depending on SDK guidance; prefer `Singleton` if it’s thread-safe.

---

## Dialog integration details
In `PromptEditorDialog.razor.cs`:
- inject:
  - `ITagSuggestionService`
  - `ISnackbar`
  - `ILogger<PromptEditorDialog>` (or logger in service only)
- add state:
  - `bool _isSuggestingTags`

Add handler:
- `private async Task SuggestTagsAsync()`
  - if already running: return
  - validate title; if invalid show snackbar `Enter a title first`
  - call `ITagSuggestionService.SuggestTagsAsync(Model.Title, ct)`
  - apply to selected tags collection via existing add/remove logic
  - snackbar success/failure

Ensure the handler uses `InvokeAsync(StateHasChanged)` as needed.

Button placement:
- next to Save/Add button in dialog footer, not in the form body.

---

## Security / compliance notes
- Treat title as user input; do not render AI output as HTML.
- Ensure tags are sanitized (letters/numbers/dash) if you want stricter consistency.
- API key must be kept in configuration providers that aren’t committed to repo.

---

## Implementation steps (files)
1. Add options:
   - `PromptHub.Web/Options/TagSuggestionOptions.cs`
   - update `PromptHub.Web/appsettings*.json`

2. Add service abstraction + OpenAI implementation:
   - `PromptHub.Web/Services/ITagSuggestionService.cs`
   - `PromptHub.Web/Services/OpenAiTagSuggestionService.cs`

3. Wire DI:
   - update `PromptHub.Web/Program.cs`

4. Update dialog UI + code-behind:
   - `PromptHub.Web/Components/Dialogs/PromptEditorDialog.razor`
   - `PromptHub.Web/Components/Dialogs/PromptEditorDialog.razor.cs`

---

## Acceptance criteria
- In `PromptEditorDialog`, a `Suggest tags` button exists and is clickable when a title is present.
- Clicking `Suggest tags` calls the backend via `ITagSuggestionService`.
- Returned tags are added into the selected tag list (deduped, lower-case, allowed-tags only, max 10).
- While the request is running, the UI indicates loading and prevents concurrent requests.
- If the request fails, a toast/snackbar is shown and a log entry is created; the dialog remains usable.
