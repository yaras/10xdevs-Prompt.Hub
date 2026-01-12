// <copyright file="PromptVotingFeature.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Microsoft.Extensions.Logging;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Application.Features.Votes;

/// <summary>
/// Default implementation of <see cref="IPromptVotingFeature" />.
/// </summary>
public sealed class PromptVotingFeature(
    IVoteStore voteStore,
    IPromptVoteAggregateStore aggregateStore,
    ILogger<PromptVotingFeature> logger) : IPromptVotingFeature
{
    /// <inheritdoc />
    public async Task<VoteResult> VoteAsync(VoteRequest request, string voterId, CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PromptId))
        {
            throw new ArgumentException("PromptId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.AuthorId))
        {
            throw new ArgumentException("AuthorId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(voterId))
        {
            throw new ArgumentException("VoterId is required.", nameof(voterId));
        }

        if (request.Requested is not (VoteValue.Like or VoteValue.Dislike))
        {
            throw new ArgumentException("Requested vote must be Like or Dislike.", nameof(request));
        }

        var existing = await voteStore.GetVoteAsync(request.PromptId, voterId, ct);
        var current = existing?.VoteValue ?? VoteValue.None;

        var newVote = ComputeNewVote(current, request.Requested);

        var (deltaLikes, deltaDislikes) = ComputeAggregateDelta(current, newVote);

        try
        {
            await voteStore.UpsertVoteAsync(request.PromptId, voterId, newVote, ct);

            var aggregates = await aggregateStore.ApplyVoteDeltaAsync(
                authorId: request.AuthorId,
                promptId: request.PromptId,
                deltaLikes: deltaLikes,
                deltaDislikes: deltaDislikes,
                ct: ct);

            return new VoteResult(newVote, aggregates.Likes, aggregates.Dislikes);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Failed to apply vote. PromptId={PromptId} VoterId={VoterId}", request.PromptId, voterId);
            throw;
        }
    }

    private static VoteValue ComputeNewVote(VoteValue current, VoteValue requested)
    {
        if (current == requested)
        {
            return VoteValue.None;
        }

        return requested;
    }

    private static (int DeltaLikes, int DeltaDislikes) ComputeAggregateDelta(VoteValue current, VoteValue next)
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
}
