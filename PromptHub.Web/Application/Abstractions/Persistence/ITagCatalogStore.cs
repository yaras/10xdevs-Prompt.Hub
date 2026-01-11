// <copyright file="ITagCatalogStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Tags;

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Provides access to the predefined tag catalog.
/// </summary>
public interface ITagCatalogStore
{
    /// <summary>
    /// Gets active tags.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of active tag items.</returns>
    Task<IReadOnlyList<TagCatalogItemModel>> GetActiveTagsAsync(CancellationToken ct);
}
