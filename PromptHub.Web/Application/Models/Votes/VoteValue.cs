namespace PromptHub.Web.Application.Models.Votes;

/// <summary>
/// Represents a user's vote value for a prompt.
/// </summary>
public enum VoteValue
{
    /// <summary>
    /// No vote.
    /// </summary>
    None = 0,

    /// <summary>
    /// Dislike.
    /// </summary>
    Dislike = -1,

    /// <summary>
    /// Like.
    /// </summary>
    Like = 1,
}
