// <copyright file="PublicPrompts.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Features.Votes;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Application.Models.Votes;
using PromptHub.Web.Components.Dialogs;
using System.Security.Claims;

namespace PromptHub.Web.Components.Pages;

/// <summary>
/// Displays public prompts and allows voting.
/// </summary>
[Authorize]
public sealed partial class PublicPrompts : ComponentBase
{
    private const int PageSize = 50;
    private const int MinSearchLength = 3;

    private ContinuationToken? continuationToken;
    private bool initialLoadCompleted;

    /// <summary>
    /// Gets the currently displayed prompts.
    /// </summary>
    protected List<PromptSummaryModel> Prompts { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the page is loading.
    /// </summary>
    protected bool IsLoading { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the next page is loading.
    /// </summary>
    protected bool IsLoadingMore { get; private set; }

    /// <summary>
    /// Gets the current error message.
    /// </summary>
    protected string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets the current search text.
    /// </summary>
    protected string? SearchText { get; set; }

    /// <summary>
    /// Gets the current user id.
    /// </summary>
    protected string? CurrentUserId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether more items can be loaded.
    /// </summary>
    protected bool CanLoadMore => this.continuationToken is not null;

    [Inject]
    private IPromptReadStore PromptReadStore { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IPromptVotingFeature PromptVotingFeature { get; set; } = default!;

    [Inject]
    private IVoteStore VoteStore { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        this.CurrentUserId = await this.TryGetUserIdAsync();
        await this.ReloadAsync();
    }

    /// <summary>
    /// Handles a vote request.
    /// </summary>
    /// <param name="request">The vote request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task HandleVoteAsync(VoteRequest request)
    {
        var voterId = this.CurrentUserId ?? await this.TryGetUserIdAsync();
        this.CurrentUserId ??= voterId;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            this.Snackbar.Add("Unable to identify the current user.", Severity.Error);
            return;
        }

        var index = this.Prompts.FindIndex(p => string.Equals(p.PromptId, request.PromptId, StringComparison.Ordinal));
        if (index < 0)
        {
            return;
        }

        var current = this.Prompts[index];
        var previous = current;

        var optimisticNewVote = current.UserVote == request.Requested ? VoteValue.None : request.Requested;
        var (dLikes, dDislikes) = ComputeDelta(current.UserVote, optimisticNewVote);

        this.Prompts[index] = current with
        {
            UserVote = optimisticNewVote,
            Likes = Math.Max(0, current.Likes + dLikes),
            Dislikes = Math.Max(0, current.Dislikes + dDislikes),
        };

        try
        {
            var result = await this.PromptVotingFeature.VoteAsync(request, voterId, CancellationToken.None);
            this.Prompts[index] = this.Prompts[index] with
            {
                UserVote = result.NewVote,
                Likes = result.Likes,
                Dislikes = result.Dislikes,
            };
        }
        catch
        {
            this.Prompts[index] = previous;
            this.Snackbar.Add("Failed to submit vote.", Severity.Error);
        }
    }

    /// <summary>
    /// Opens the view prompt dialog.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task OpenViewDialogAsync(string promptId)
    {
        var parameters = new DialogParameters<PromptViewerDialog>
        {
            { d => d.PromptId, promptId },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        await this.DialogService.ShowAsync<PromptViewerDialog>("View prompt", parameters, options);
    }

    /// <summary>
    /// Loads the next page of public prompts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
            var filtered = this.ApplyConstrainedSearch(page.Items);
            var withVotes = await this.PopulateVotesAsync(filtered, CancellationToken.None);
            this.Prompts.AddRange(withVotes);
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

    /// <summary>
    /// Updates the search text and reloads results.
    /// </summary>
    /// <param name="value">The new search input.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task OnSearchTextChangedAsync(string? value)
    {
        this.SearchText = value;
        await this.ReloadAsync();
    }

    private static (int DeltaLikes, int DeltaDislikes) ComputeDelta(VoteValue current, VoteValue next)
    {
        var deltaLikes = 0;
        var deltaDislikes = 0;

        if (current == VoteValue.Like)
        {
            deltaLikes -= 1;
        }
        else if (current == VoteValue.Dislike)
        {
            deltaDislikes -= 1;
        }

        if (next == VoteValue.Like)
        {
            deltaLikes += 1;
        }
        else if (next == VoteValue.Dislike)
        {
            deltaDislikes += 1;
        }

        return (deltaLikes, deltaDislikes);
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
            var filtered = this.ApplyConstrainedSearch(page.Items);
            var withVotes = await this.PopulateVotesAsync(filtered, CancellationToken.None);
            this.Prompts.AddRange(withVotes);
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

    private async Task<IReadOnlyList<PromptSummaryModel>> PopulateVotesAsync(IReadOnlyList<PromptSummaryModel> items, CancellationToken ct)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var voterId = this.CurrentUserId ?? await this.TryGetUserIdAsync();
        this.CurrentUserId ??= voterId;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return items;
        }

        var result = new PromptSummaryModel[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var vote = await this.VoteStore.GetVoteAsync(item.PromptId, voterId, ct);
            result[i] = item with { UserVote = vote?.VoteValue ?? VoteValue.None };
        }

        return result;
    }

    private async Task<string?> TryGetUserIdAsync()
    {
        try
        {
            var authState = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var id = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
        catch
        {
            return null;
        }
    }
}
