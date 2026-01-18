// <copyright file="MyPrompts.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Components.Dialogs;

namespace PromptHub.Web.Components.Pages;

/// <summary>
/// Displays the current user's prompts.
/// </summary>
[Authorize]
public sealed partial class MyPrompts : ComponentBase
{
    private const int PageSize = 50;

    private ContinuationToken? continuationToken;
    private bool initialLoadCompleted;

    /// <summary>
    /// Gets the prompts for the current author.
    /// </summary>
    private List<PromptSummaryModel> Prompts { get; } = new();

    /// <summary>
    /// Gets or sets the current user's author id.
    /// </summary>
    private string? CurrentUserAuthorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page is loading.
    /// </summary>
    private bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the next page is loading.
    /// </summary>
    private bool IsLoadingMore { get; set; }

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether more items can be loaded.
    /// </summary>
    private bool CanLoadMore => this.continuationToken is not null;

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        this.CurrentUserAuthorId = await this.TryGetAuthorIdAsync();
        await this.ReloadAsync();
    }

    /// <summary>
    /// Loads the next page of prompts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LoadMoreAsync()
    {
        if (this.IsLoading || this.IsLoadingMore || this.continuationToken is null)
        {
            return;
        }

        try
        {
            this.IsLoadingMore = true;
            this.ErrorMessage = null;

            var authorId = this.CurrentUserAuthorId ?? await this.TryGetAuthorIdAsync();
            this.CurrentUserAuthorId ??= authorId;
            if (authorId is null)
            {
                return;
            }

            var page = await this.PromptReadStore.ListMyPromptsAsync(authorId, this.continuationToken, PageSize, CancellationToken.None);
            this.Prompts.AddRange(page.Items);
            this.continuationToken = page.ContinuationToken;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to load your prompts.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoadingMore = false;
        }
    }

    /// <summary>
    /// Opens the create prompt dialog.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OpenCreateDialogAsync()
    {
        var authorId = await this.TryGetAuthorIdAsync();
        if (authorId is null)
        {
            return;
        }

        var authorEmail = await this.TryGetAuthorEmailAsync();

        var parameters = new DialogParameters<PromptEditorDialog>
        {
            { d => d.Mode, PromptEditorMode.Create },
            { d => d.AuthorId, authorId },
            { d => d.AuthorEmail, authorEmail },
            { d => d.Model, new PromptEditorModel { Visibility = PromptVisibility.Private } },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, FullWidth = true, MaxWidth = MaxWidth.Medium };
        var dialog = await this.DialogService.ShowAsync<PromptEditorDialog>("Create prompt", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await this.ReloadAsync();
        }
    }

    /// <summary>
    /// Opens the view prompt dialog.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OpenViewDialogAsync(string promptId)
    {
        var authorId = await this.TryGetAuthorIdAsync();
        if (authorId is null)
        {
            return;
        }

        var parameters = new DialogParameters<PromptViewerDialog>
        {
            { d => d.AuthorId, authorId },
            { d => d.PromptId, promptId },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        await this.DialogService.ShowAsync<PromptViewerDialog>("View prompt", parameters, options);
    }

    /// <summary>
    /// Opens the edit prompt dialog.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OpenEditDialogAsync(string promptId)
    {
        var authorId = await this.TryGetAuthorIdAsync();
        if (authorId is null)
        {
            return;
        }

        var parameters = new DialogParameters<PromptEditorDialog>
        {
            { d => d.Mode, PromptEditorMode.Edit },
            { d => d.AuthorId, authorId },
            { d => d.PromptId, promptId },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await this.DialogService.ShowAsync<PromptEditorDialog>("Edit prompt", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await this.ReloadAsync();
        }
    }

    private async Task ReloadAsync()
    {
        if (this.IsLoading)
        {
            return;
        }

        try
        {
            this.IsLoading = true;
            this.ErrorMessage = null;

            var authorId = this.CurrentUserAuthorId ?? await this.TryGetAuthorIdAsync();
            this.CurrentUserAuthorId ??= authorId;
            if (authorId is null)
            {
                return;
            }

            var page = await this.PromptReadStore.ListMyPromptsAsync(authorId, token: null, PageSize, CancellationToken.None);

            this.Prompts.Clear();
            this.Prompts.AddRange(page.Items);
            this.continuationToken = page.ContinuationToken;
            this.initialLoadCompleted = true;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to load your prompts.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoading = false;

            if (!this.initialLoadCompleted && string.IsNullOrWhiteSpace(this.ErrorMessage))
            {
                this.ErrorMessage = "Unable to load prompts.";
            }
        }
    }

    private async Task<string?> TryGetAuthorIdAsync()
    {
        try
        {
            var authState = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var authorId = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId))
            {
                this.ErrorMessage = "Unable to identify the current user.";
                return null;
            }

            return authorId;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Unable to identify the current user.";
            Console.Error.WriteLine(ex);
            return null;
        }
    }

    private async Task<string?> TryGetAuthorEmailAsync()
    {
        try
        {
            var authState = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var email = user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue("email")
                ?? user.FindFirstValue("preferred_username")
                ?? user.FindFirstValue("upn");

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return email.Trim();
        }
        catch
        {
            return null;
        }
    }
}
