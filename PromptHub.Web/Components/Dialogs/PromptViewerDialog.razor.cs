// <copyright file="PromptViewerDialog.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using MudBlazor;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Components.Dialogs;

/// <summary>
/// Read-only prompt viewer dialog.
/// </summary>
public sealed partial class PromptViewerDialog : ComponentBase
{
    /// <summary>
    /// Gets or sets the author id used to view an author's private prompt.
    /// If omitted, the prompt is loaded from the public store.
    /// </summary>
    [Parameter]
    public string? AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the prompt id.
    /// </summary>
    [Parameter]
    public string? PromptId { get; set; }

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is currently loading.
    /// </summary>
    private bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the loaded prompt.
    /// </summary>
    private PromptModel? Prompt { get; set; }

    /// <inheritdoc />
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(this.PromptId))
        {
            this.ErrorMessage = "Missing prompt context.";
            return;
        }

        try
        {
            this.IsLoading = true;

            this.Prompt = string.IsNullOrWhiteSpace(this.AuthorId)
                ? await this.PromptReadStore.GetPublicByIdAsync(this.PromptId, CancellationToken.None)
                : await this.PromptReadStore.GetByIdForAuthorAsync(this.AuthorId, this.PromptId, CancellationToken.None);

            if (this.Prompt is null)
            {
                this.ErrorMessage = "Prompt not found.";
            }
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

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    private void Close() => this.Dialog.Close(DialogResult.Ok(true));
}
