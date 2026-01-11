// <copyright file="TitleSearchIndexEntity.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

/// <summary>
/// Represents a row in the <c>TitleSearchIndex</c> Azure Table.
/// </summary>
public sealed class TitleSearchIndexEntity : ITableEntity
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
    /// Gets or sets the token value.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt identifier.
    /// </summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the visibility value (<c>private</c> or <c>public</c>).
    /// </summary>
    public string Visibility { get; set; } = "private";

    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the prompt is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
