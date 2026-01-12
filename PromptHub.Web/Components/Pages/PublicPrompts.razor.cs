// <copyright file="PublicPrompts.razor.cs" company="PromptHub">
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

[Authorize]
public sealed partial class PublicPrompts : ComponentBase
{
    private const int PageSize = 50;
    private const int MinSearchLength = 3;

    private ContinuationToken? continuationToken;
    private bool initialLoadCompleted;

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected string? CurrentUserAuthorId { get; private set; }

    protected List<PromptSummaryModel> Prompts { get; } = new();

    protected bool IsLoading { get; private set; }

    protected bool IsLoadingMore { get; private set; }

    protected string? ErrorMessage { get; private set; }

    protected string? SearchText { get; set; }

    protected bool CanLoadMore => this.continuationToken is not null;

    protected override async Task OnInitializedAsync()
    {
        this.CurrentUserAuthorId = await this.TryGetAuthorIdAsync();
        await this.ReloadAsync();
    }

    protected async Task OpenViewDialogAsync(string promptId)
    {
        var authorId = this.CurrentUserAuthorId ?? await this.TryGetAuthorIdAsync();
        this.CurrentUserAuthorId ??= authorId;
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

    protected async Task LoadMoreAsync()
    {
        if (this.IsLoading || this.IsLoadingMore || this.continuationToken is null)
        {
            return;
        }

        try
        {
            this.IsLoadingMore = true;
            this.ErrorMessage = null;

            var page = await this.PromptReadStore.ListPublicNewestAsync(this.continuationToken, PageSize, CancellationToken.None);
            this.Prompts.AddRange(this.ApplyConstrainedSearch(page.Items));
            this.continuationToken = page.ContinuationToken;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to load public prompts.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoadingMore = false;
        }
    }

    protected async Task OnSearchTextChangedAsync(string? value)
    {
        this.SearchText = value;
        await this.ReloadAsync();
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

            var page = await this.PromptReadStore.ListPublicNewestAsync(token: null, PageSize, CancellationToken.None);

            this.Prompts.Clear();
            this.Prompts.AddRange(this.ApplyConstrainedSearch(page.Items));
            this.continuationToken = page.ContinuationToken;
            this.initialLoadCompleted = true;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Failed to load public prompts.";
            Console.Error.WriteLine(ex);
        }
        finally
        {
            this.IsLoading = false;

            if (!this.initialLoadCompleted && string.IsNullOrWhiteSpace(this.ErrorMessage))
            {
                this.ErrorMessage = "Unable to load public prompts.";
            }
        }
    }

    private IReadOnlyList<PromptSummaryModel> ApplyConstrainedSearch(IReadOnlyList<PromptSummaryModel> items)
    {
        var q = (this.SearchText ?? string.Empty).Trim();
        if (q.Length < MinSearchLength)
        {
            return items;
        }

        q = q.ToLowerInvariant();

        return items
            .Where(p => (p.Title ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToArray();
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
}
