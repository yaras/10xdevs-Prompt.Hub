// <copyright file="PromptModel.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Models.Prompts;

/// <summary>
/// Represents a prompt in the application layer.
/// </summary>
/// <param name="PromptId">The prompt identifier (ULID stored as a string).</param>
/// <param name="AuthorId">The stable author identifier (typically Entra <c>oid</c>).</param>
/// <param name="Title">The prompt title.</param>
/// <param name="PromptText">The prompt content.</param>
/// <param name="Tags">The normalized, lower-case tags.</param>
/// <param name="Visibility">The prompt visibility.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="UpdatedAt">The last update timestamp.</param>
/// <param name="Likes">The aggregate likes count.</param>
/// <param name="Dislikes">The aggregate dislikes count.</param>
/// <param name="ETag">The entity tag used for optimistic concurrency.</param>
public sealed record PromptModel(
    string PromptId,
    string AuthorId,
    string Title,
    string PromptText,
    IReadOnlyList<string> Tags,
    PromptVisibility Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Likes,
    int Dislikes,
    string? ETag = null);
