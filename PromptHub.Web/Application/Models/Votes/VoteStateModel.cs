namespace PromptHub.Web.Application.Models.Votes;

public sealed record VoteStateModel(
	string PromptId,
	string VoterId,
	VoteValue VoteValue,
	DateTimeOffset UpdatedAt,
	string? ETag = null);
