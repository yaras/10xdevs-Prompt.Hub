// <copyright file="PromptSummaryModel.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Models.Prompts;

/// <summary>
/// Represents a prompt summary used for list rendering.
/// </summary>
/// <param name="PromptId">The prompt identifier (ULID stored as a string).</param>
/// <param name="AuthorId">The stable author identifier (typically Entra <c>oid</c>).</param>
/// <param name="Title">The prompt title.</param>
/// <param name="Tags">The normalized, lower-case tags.</param>
/// <param name="Visibility">The prompt visibility.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="UpdatedAt">The last update timestamp.</param>
/// <param name="Likes">The aggregate likes count.</param>
/// <param name="Dislikes">The aggregate dislikes count.</param>
public sealed record PromptSummaryModel(
    string PromptId,
    string AuthorId,
    string Title,
    IReadOnlyList<string> Tags,
    PromptVisibility Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Likes,
    int Dislikes);
