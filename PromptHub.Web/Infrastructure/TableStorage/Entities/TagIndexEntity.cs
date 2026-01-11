// <copyright file="TagIndexEntity.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

/// <summary>
/// Represents a row in the <c>TagIndex</c> Azure Table.
/// </summary>
public sealed class TagIndexEntity : ITableEntity
{
    /// <inheritdoc />
    public string PartitionKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc />
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets or sets the tag value.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt identifier.
    /// </summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the visibility value (<c>private</c> or <c>public</c>).
    /// </summary>
    public string Visibility { get; set; } = "private";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the aggregate likes count.
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// Gets or sets the aggregate dislikes count.
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prompt is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
