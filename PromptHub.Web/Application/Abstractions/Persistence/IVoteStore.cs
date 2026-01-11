using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Application.Abstractions.Persistence;

public interface IVoteStore
{
	Task<VoteStateModel?> GetVoteAsync(string promptId, string voterId, CancellationToken ct);
	Task<VoteStateModel> UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct);
}
