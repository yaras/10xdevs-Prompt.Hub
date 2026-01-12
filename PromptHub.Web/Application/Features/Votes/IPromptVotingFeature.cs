// <copyright file="IPromptVotingFeature.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Application.Features.Votes;

/// <summary>
/// Coordinates vote state transitions and aggregate updates.
/// </summary>
public interface IPromptVotingFeature
{
    /// <summary>
    /// Applies a vote request for a prompt.
    /// </summary>
    /// <param name="request">The vote request.</param>
    /// <param name="voterId">The stable voter identifier (typically Entra <c>oid</c>).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated vote and aggregate count.</returns>
    Task<VoteResult> VoteAsync(VoteRequest request, string voterId, CancellationToken ct);
}
