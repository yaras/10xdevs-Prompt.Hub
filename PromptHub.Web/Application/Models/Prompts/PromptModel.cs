namespace PromptHub.Web.Application.Models.Prompts;

public sealed record PromptModel(
	string PromptId,
	string AuthorId,
	string Title,
	string PromptText,
	IReadOnlyList<string> Tags,
	PromptVisibility Visibility,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	int Likes,
	int Dislikes,
	string? ETag = null);
