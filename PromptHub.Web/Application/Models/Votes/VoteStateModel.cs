// <copyright file="VoteStateModel.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Models.Votes;

/// <summary>
/// Represents the current vote state for a prompt by a specific voter.
/// </summary>
/// <param name="PromptId">The prompt identifier.</param>
/// <param name="VoterId">The stable voter identifier (typically Entra <c>oid</c>).</param>
/// <param name="VoteValue">The vote value.</param>
/// <param name="UpdatedAt">The last update timestamp.</param>
/// <param name="ETag">The entity tag used for optimistic concurrency.</param>
public sealed record VoteStateModel(
    string PromptId,
    string VoterId,
    VoteValue VoteValue,
    DateTimeOffset UpdatedAt,
    string? ETag = null);
