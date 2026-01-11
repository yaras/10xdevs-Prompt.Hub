using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Provides read/write operations for prompt votes.
/// </summary>
public interface IVoteStore
{
    /// <summary>
    /// Gets a vote state for a prompt and voter.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="voterId">The voter id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The vote state if found; otherwise <see langword="null" />.</returns>
    Task<VoteStateModel?> GetVoteAsync(string promptId, string voterId, CancellationToken ct);

    /// <summary>
    /// Inserts or updates a vote state for a prompt and voter.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <param name="voterId">The voter id.</param>
    /// <param name="voteValue">The new vote value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stored vote state.</returns>
    Task<VoteStateModel> UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct);
}
