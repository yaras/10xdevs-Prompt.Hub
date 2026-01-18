// <copyright file="PromptEditorDialog.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using MudBlazor;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Application.TagSuggestions;

namespace PromptHub.Web.Components.Dialogs;

/// <summary>
/// Dialog for creating or editing a prompt.
/// </summary>
public sealed partial class PromptEditorDialog : ComponentBase
{
    private string? expectedETag;
    private bool isSuggestingTags;

    /// <summary>
    /// Gets or sets the editor mode.
    /// </summary>
    [Parameter]
    public PromptEditorMode Mode { get; set; }

    /// <summary>
    /// Gets or sets the author id (required for edit mode).
    /// </summary>
    [Parameter]
    public string? AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the author email address (optional).
    /// </summary>
    [Parameter]
    public string? AuthorEmail { get; set; }

    /// <summary>
    /// Gets or sets the prompt id (required for edit mode).
    /// </summary>
    [Parameter]
    public string? PromptId { get; set; }

    /// <summary>
    /// Gets or sets the editable model.
    /// </summary>
    [Parameter]
    public PromptEditorModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the prompt read store.
    /// </summary>
    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    /// <summary>
    /// Gets or sets the prompt write store.
    /// </summary>
    [Inject]
    private IPromptWriteStore PromptWriteStore { get; set; } = default!;

    /// <summary>
    /// Gets or sets the tag suggestion service.
    /// </summary>
    [Inject]
    private ITagSuggestionService TagSuggestionService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service.
    /// </summary>
    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    [Inject]
    private ILogger<PromptEditorDialog> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current dialog instance.
    /// </summary>
    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    /// <summary>
    /// Gets or sets the form reference used for validation.
    /// </summary>
    private MudForm? Form { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is currently loading.
    /// </summary>
    private bool IsLoading
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether tag suggestions are currently in progress.
    /// </summary>
    private bool IsSuggestingTags => this.isSuggestingTags;

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    private string? ErrorMessage
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the pending new tag value.
    /// </summary>
    private string? NewTag { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (this.Mode != PromptEditorMode.Edit)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(this.AuthorId) || string.IsNullOrWhiteSpace(this.PromptId))
        {
            this.ErrorMessage = "Missing prompt context.";
            return;
        }

        try
        {
            this.IsLoading = true;
            var prompt = await this.PromptReadStore.GetByIdForAuthorAsync(this.AuthorId, this.PromptId, CancellationToken.None);
            if (prompt is null)
            {
                this.ErrorMessage = "Prompt not found.";
                return;
            }

            this.expectedETag = prompt.ETag;
            this.Model = new PromptEditorModel
            {
                Title = prompt.Title,
                PromptText = prompt.PromptText,
                Visibility = prompt.Visibility,
                Tags = prompt.Tags.ToList(),
            };
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to load prompt.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    private static IEnumerable<string> MaxCharacters(string ch, int max)
    {
        if (!string.IsNullOrEmpty(ch) && max < ch?.Length)
        {
            yield return $"Max {max} characters";
        }
    }

    /// <summary>
    /// Cancels the dialog.
    /// </summary>
    private void Cancel() => this.Dialog.Cancel();

    /// <summary>
    /// Adds <see cref="NewTag"/> to the tag list if valid.
    /// </summary>
    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(this.NewTag))
        {
            return;
        }

        var normalized = this.NewTag.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return;
        }

        if (!this.Model.Tags.Contains(normalized, StringComparer.Ordinal))
        {
            this.Model.Tags.Add(normalized);
        }

        this.NewTag = null;
        this.StateHasChanged();
    }

    /// <summary>
    /// Requests tag suggestions and adds any new suggestions to the prompt.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SuggestTagsAsync()
    {
        if (this.isSuggestingTags)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(this.Model.Title))
        {
            this.Snackbar.Add("Enter a title first.", Severity.Info);
            return;
        }

        if (this.Model.Tags.Count >= 10)
        {
            this.Snackbar.Add("You already have the maximum number of tags.", Severity.Info);
            return;
        }

        try
        {
            this.isSuggestingTags = true;
            var suggested = await this.TagSuggestionService.SuggestTagsAsync(this.Model.Title, CancellationToken.None);

            var added = 0;
            foreach (var tag in suggested)
            {
                if (this.Model.Tags.Count >= 10)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                this.NewTag = tag;
                var beforeCount = this.Model.Tags.Count;
                this.AddTag();

                if (this.Model.Tags.Count > beforeCount)
                {
                    added++;
                }
            }

            if (added > 0)
            {
                this.Snackbar.Add($"Added {added} suggested tag(s).", Severity.Success);
            }
            else
            {
                this.Snackbar.Add("No new tags were suggested.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Tag suggestion failed. Mode={Mode} PromptId={PromptId}", this.Mode, this.PromptId);
            this.Snackbar.Add("Unable to suggest tags right now. Please try again.", Severity.Error);
        }
        finally
        {
            this.isSuggestingTags = false;
            await this.InvokeAsync(this.StateHasChanged);
        }
    }

    /// <summary>
    /// Removes the specified tag from the model.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    private void RemoveTag(string tag) => this.Model.Tags.Remove(tag);

    /// <summary>
    /// Saves the prompt.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SaveAsync()
    {
        this.ErrorMessage = null;

        if (this.Form is null)
        {
            return;
        }

        await this.Form.Validate();
        if (!this.Form.IsValid)
        {
            return;
        }

        try
        {
            this.IsLoading = true;

            if (this.Mode == PromptEditorMode.Create)
            {
                var authorId = this.AuthorId;
                if (string.IsNullOrWhiteSpace(authorId))
                {
                    this.ErrorMessage = "Missing author context.";
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                var prompt = new PromptModel(
                    PromptId: string.Empty,
                    AuthorId: authorId,
                    Title: this.Model.Title,
                    PromptText: this.Model.PromptText,
                    AuthorEmail: this.AuthorEmail,
                    Tags: this.Model.Tags,
                    Visibility: this.Model.Visibility,
                    CreatedAt: now,
                    UpdatedAt: now,
                    Likes: 0,
                    Dislikes: 0,
                    ETag: null);

                await this.PromptWriteStore.CreateAsync(prompt, CancellationToken.None);
                this.Dialog.Close(DialogResult.Ok(true));
                return;
            }

            if (string.IsNullOrWhiteSpace(this.AuthorId) || string.IsNullOrWhiteSpace(this.PromptId))
            {
                this.ErrorMessage = "Missing prompt context.";
                return;
            }

            if (string.IsNullOrWhiteSpace(this.expectedETag))
            {
                this.ErrorMessage = "This prompt has changed. Please reload and try again.";
                return;
            }

            var existing = await this.PromptReadStore.GetByIdForAuthorAsync(this.AuthorId, this.PromptId, CancellationToken.None);
            if (existing is null)
            {
                this.ErrorMessage = "Prompt not found.";
                return;
            }

            var updated = existing with
            {
                Title = this.Model.Title,
                PromptText = this.Model.PromptText,
                Tags = this.Model.Tags.ToList(),
                Visibility = this.Model.Visibility,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            try
            {
                await this.PromptWriteStore.UpdateAsync(updated, this.expectedETag, CancellationToken.None);
                this.Dialog.Close(DialogResult.Ok(true));
            }
            catch
            {
                this.ErrorMessage = "This prompt was updated elsewhere. Please reload and try again.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to save prompt.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoading = false;
        }
    }
}
