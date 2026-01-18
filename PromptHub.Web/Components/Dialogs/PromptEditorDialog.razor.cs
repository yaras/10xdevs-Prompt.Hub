// <copyright file="PromptEditorDialog.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MudBlazor;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Application.TagSuggestions;
using MudBlazor.Components;

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

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [Inject]
    private IPromptWriteStore PromptWriteStore { get; set; } = default!;

    [Inject]
    private ITagSuggestionService TagSuggestionService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private ILogger<PromptEditorDialog> Logger { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    protected MudForm? Form { get; set; }

    protected bool IsLoading { get; private set; }

    protected bool IsSuggestingTags => this.isSuggestingTags;

    protected string? ErrorMessage { get; private set; }

    protected string? NewTag { get; set; }

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

    protected void Cancel() => this.Dialog.Cancel();

    protected void AddTag()
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

    protected async Task SuggestTagsAsync()
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

    protected void RemoveTag(string tag) => this.Model.Tags.Remove(tag);

    protected async Task SaveAsync()
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

    private static IEnumerable<string> MaxCharacters(string ch, int max)
    {
        if (!string.IsNullOrEmpty(ch) && max < ch?.Length)
        {
            yield return $"Max {max} characters";
        }
    }
}

/// <summary>
/// Editor dialog mode.
/// </summary>
public enum PromptEditorMode
{
    /// <summary>
    /// Create mode.
    /// </summary>
    Create,

    /// <summary>
    /// Edit mode.
    /// </summary>
    Edit,
}

/// <summary>
/// Editable prompt fields.
/// </summary>
public sealed class PromptEditorModel
{
    /// <summary>
    /// Gets or sets the prompt title.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt text.
    /// </summary>
    [Required]
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the prompt visibility.
    /// </summary>
    public PromptVisibility Visibility { get; set; } = PromptVisibility.Private;
}
