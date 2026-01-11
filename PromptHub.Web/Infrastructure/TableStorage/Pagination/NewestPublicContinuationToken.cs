// <copyright file="NewestPublicContinuationToken.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.Text.Json;

namespace PromptHub.Web.Infrastructure.TableStorage.Pagination;

/// <summary>
/// Represents the continuation state for the public newest listing.
/// </summary>
/// <param name="Bucket">The current month bucket (yyyyMM).</param>
/// <param name="Continuation">The SDK continuation token within the bucket.</param>
public sealed record NewestPublicContinuationToken(string Bucket, string? Continuation)
{
    /// <summary>
    /// Serializes a token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The serialized value.</returns>
    public static string Serialize(NewestPublicContinuationToken token) => JsonSerializer.Serialize(token);

    /// <summary>
    /// Deserializes a token.
    /// </summary>
    /// <param name="value">The serialized value.</param>
    /// <returns>The deserialized token.</returns>
    public static NewestPublicContinuationToken Deserialize(string value) =>
        JsonSerializer.Deserialize<NewestPublicContinuationToken>(value)
        ?? throw new InvalidOperationException("Invalid continuation token.");
}
