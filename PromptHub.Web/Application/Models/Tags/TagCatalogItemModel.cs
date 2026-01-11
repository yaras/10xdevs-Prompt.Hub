// <copyright file="TagCatalogItemModel.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Models.Tags;

/// <summary>
/// Represents a tag option from the tag catalog.
/// </summary>
/// <param name="Tag">The normalized tag (lower-case).</param>
/// <param name="IsActive">Whether the tag is active and can be used.</param>
/// <param name="DisplayName">Optional display name.</param>
/// <param name="SortOrder">Optional sort order.</param>
public sealed record TagCatalogItemModel(
    string Tag,
    bool IsActive,
    string? DisplayName = null,
    int? SortOrder = null);
