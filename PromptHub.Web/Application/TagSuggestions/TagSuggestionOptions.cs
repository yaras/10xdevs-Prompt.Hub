// <copyright file="TagSuggestionOptions.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.TagSuggestions;

/// <summary>
/// Configuration options for AI-based tag suggestions.
/// </summary>
public sealed class TagSuggestionOptions
{
    /// <summary>
    /// Gets or sets the list of allowed tags that can be suggested.
    /// </summary>
    public string[] AllowedTags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum number of tags to suggest.
    /// </summary>
    public int MaxSuggestions { get; set; } = 4;

    /// <summary>
    /// Gets or sets the OpenAI model name.
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";
}
