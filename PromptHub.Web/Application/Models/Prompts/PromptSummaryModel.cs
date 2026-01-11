namespace PromptHub.Web.Application.Models.Prompts;

public sealed record PromptSummaryModel(
	string PromptId,
	string AuthorId,
	string Title,
	IReadOnlyList<string> Tags,
	PromptVisibility Visibility,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	int Likes,
	int Dislikes);
