namespace PromptHub.Web.Application.Models.Prompts;

/// <summary>
/// Represents the visibility of a prompt.
/// </summary>
public enum PromptVisibility
{
    /// <summary>
    /// The prompt is visible only to its author.
    /// </summary>
    Private = 0,

    /// <summary>
    /// The prompt is visible to all authenticated users.
    /// </summary>
    Public = 1,
}
