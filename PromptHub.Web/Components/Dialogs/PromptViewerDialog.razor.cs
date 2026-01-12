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
    [Parameter]
    public string? AuthorId { get; set; }

    [Parameter]
    public string? PromptId { get; set; }

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    protected bool IsLoading { get; private set; }

    protected string? ErrorMessage { get; private set; }

    protected PromptModel? Prompt { get; private set; }

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

    protected void Close() => this.Dialog.Close(DialogResult.Ok(true));
}
