// <copyright file="PublicPromptsNewestIndexEntity.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

/// <summary>
/// Represents a row in the <c>PublicPromptsNewestIndex</c> Azure Table.
/// </summary>
public sealed class PublicPromptsNewestIndexEntity : ITableEntity
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
    /// Gets or sets the prompt identifier.
    /// </summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized prompt title.
    /// </summary>
    public string TitleNormalized { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized tags as a delimited string.
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the aggregate likes count.
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// Gets or sets the aggregate dislikes count.
    /// </summary>
    public int Dislikes { get; set; }
}
