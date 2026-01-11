// <copyright file="PromptVoteEntity.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

/// <summary>
/// Represents a row in the <c>PromptVotes</c> Azure Table.
/// </summary>
public sealed class PromptVoteEntity : ITableEntity
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
    /// Gets or sets the voter identifier.
    /// </summary>
    public string VoterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vote value (-1, 0, 1).
    /// </summary>
    public int VoteValue { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
