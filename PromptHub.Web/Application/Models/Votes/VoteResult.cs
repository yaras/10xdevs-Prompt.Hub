// <copyright file="VoteResult.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Models.Votes;

/// <summary>
/// Represents the result of applying a vote.
/// </summary>
/// <param name="NewVote">The new vote value stored for the user.</param>
/// <param name="Likes">The updated total likes count.</param>
/// <param name="Dislikes">The updated total dislikes count.</param>
public sealed record VoteResult(
    VoteValue NewVote,
    int Likes,
    int Dislikes);
