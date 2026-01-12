// <copyright file="VoteRequest.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Application.Models.Votes;

/// <summary>
/// Represents a vote request initiated by the UI.
/// </summary>
/// <param name="PromptId">The prompt identifier.</param>
/// <param name="AuthorId">The prompt author's identifier.</param>
/// <param name="Requested">The requested vote value (Like or Dislike).</param>
public sealed record VoteRequest(
    string PromptId,
    string AuthorId,
    VoteValue Requested);
