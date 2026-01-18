// <copyright file="PromptEditorModel.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Components.Dialogs;

/// <summary>
/// Editable prompt fields.
/// </summary>
public sealed class PromptEditorModel
{
    /// <summary>
    /// Gets or sets the prompt title.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt text.
    /// </summary>
    [Required]
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the prompt visibility.
    /// </summary>
    public PromptVisibility Visibility { get; set; } = PromptVisibility.Private;
}
