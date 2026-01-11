// <copyright file="IPromptReadStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Provides read operations for prompts.
/// </summary>
public interface IPromptReadStore
{
    /// <summary>
    /// Gets a prompt by id for the given author.
    /// </summary>
    /// <param name="authorId">The author id.</param>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The prompt if found; otherwise <see langword="null" />.</returns>
    Task<PromptModel?> GetByIdForAuthorAsync(string authorId, string promptId, CancellationToken ct);

    /// <summary>
    /// Gets a public prompt by id.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The prompt if found and visible; otherwise <see langword="null" />.</returns>
    Task<PromptModel?> GetPublicByIdAsync(string promptId, CancellationToken ct);

    /// <summary>
    /// Lists prompts in the author's partition.
    /// </summary>
    /// <param name="authorId">The author id.</param>
    /// <param name="token">The continuation token.</param>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page of prompt summaries.</returns>
    Task<ContinuationPage<PromptSummaryModel>> ListMyPromptsAsync(string authorId, ContinuationToken? token, int pageSize, CancellationToken ct);

    /// <summary>
    /// Lists public prompts ordered by newest first.
    /// </summary>
    /// <param name="token">The continuation token.</param>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page of prompt summaries.</returns>
    Task<ContinuationPage<PromptSummaryModel>> ListPublicNewestAsync(ContinuationToken? token, int pageSize, CancellationToken ct);
}
