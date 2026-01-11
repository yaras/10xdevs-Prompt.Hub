// <copyright file="IPromptWriteStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Provides write operations for prompts.
/// </summary>
public interface IPromptWriteStore
{
    /// <summary>
    /// Creates a prompt.
    /// </summary>
    /// <param name="prompt">The prompt model.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created prompt, including server-generated values.</returns>
    Task<PromptModel> CreateAsync(PromptModel prompt, CancellationToken ct);

    /// <summary>
    /// Updates a prompt using optimistic concurrency.
    /// </summary>
    /// <param name="prompt">The prompt model.</param>
    /// <param name="expectedETag">The expected ETag value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated prompt.</returns>
    Task<PromptModel> UpdateAsync(PromptModel prompt, string expectedETag, CancellationToken ct);

    /// <summary>
    /// Soft deletes a prompt.
    /// </summary>
    /// <param name="authorId">The author id.</param>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SoftDeleteAsync(string authorId, string promptId, CancellationToken ct);
}
