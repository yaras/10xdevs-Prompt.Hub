// <copyright file="ITagSuggestionService.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.TagSuggestions;

/// <summary>
/// Suggests tags based on prompt information.
/// </summary>
public interface ITagSuggestionService
{
    /// <summary>
    /// Suggests tags for the provided prompt title.
    /// </summary>
    /// <param name="title">Prompt title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested tag candidates.</returns>
    Task<IReadOnlyList<string>> SuggestTagsAsync(string title, CancellationToken cancellationToken);
}
