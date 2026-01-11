// <copyright file="PromptEntityMapper.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

/// <summary>
/// Maps between <see cref="PromptEntity" /> and <see cref="PromptModel" />.
/// </summary>
public static class PromptEntityMapper
{
    /// <summary>
    /// Converts a storage entity into an application model.
    /// </summary>
    /// <param name="entity">The storage entity.</param>
    /// <returns>The application model.</returns>
    public static PromptModel ToModel(PromptEntity entity) => new(
        PromptId: entity.PromptId,
        AuthorId: entity.AuthorId,
        Title: entity.Title,
        PromptText: entity.PromptText,
        Tags: TagString.ToTags(entity.Tags),
        Visibility: PromptVisibilityMapper.ToModel(entity.Visibility),
        CreatedAt: entity.CreatedAt,
        UpdatedAt: entity.UpdatedAt,
        Likes: entity.Likes,
        Dislikes: entity.Dislikes,
        ETag: entity.ETag.ToString());

    /// <summary>
    /// Converts an application model into a storage entity.
    /// </summary>
    /// <param name="model">The application model.</param>
    /// <returns>The storage entity.</returns>
    public static PromptEntity FromModel(PromptModel model)
    {
        var now = DateTimeOffset.UtcNow;
        var title = model.Title.Trim();

        return new PromptEntity
        {
            PartitionKey = KeyFormat.PromptsPartitionKey(model.AuthorId),
            RowKey = model.PromptId,
            PromptId = model.PromptId,
            AuthorId = model.AuthorId,
            Title = title,
            TitleNormalized = title.ToLowerInvariant(),
            PromptText = model.PromptText,
            Tags = TagString.ToDelimited(model.Tags),
            Visibility = PromptVisibilityMapper.ToStorage(model.Visibility),
            CreatedAt = model.CreatedAt == default ? now : model.CreatedAt,
            UpdatedAt = model.UpdatedAt == default ? now : model.UpdatedAt,
            IsDeleted = false,
            Likes = model.Likes,
            Dislikes = model.Dislikes,
        };
    }
}
