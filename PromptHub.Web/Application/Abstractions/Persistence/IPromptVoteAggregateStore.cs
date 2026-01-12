// <copyright file="IPromptVoteAggregateStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Provides operations to update likes/dislikes aggregates for a prompt.
/// </summary>
public interface IPromptVoteAggregateStore
{
    /// <summary>
    /// Applies a vote delta to the canonical prompt aggregates using optimistic concurrency.
    /// </summary>
    /// <param name="authorId">The prompt author id.</param>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="deltaLikes">The likes delta (may be negative).</param>
    /// <param name="deltaDislikes">The dislikes delta (may be negative).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated aggregates.</returns>
    Task<PromptAggregatesModel> ApplyVoteDeltaAsync(
        string authorId,
        string promptId,
        int deltaLikes,
        int deltaDislikes,
        CancellationToken ct);
}

/// <summary>
/// Prompt aggregate counts.
/// </summary>
/// <param name="Likes">The likes count.</param>
/// <param name="Dislikes">The dislikes count.</param>
public sealed record PromptAggregatesModel(int Likes, int Dislikes);
