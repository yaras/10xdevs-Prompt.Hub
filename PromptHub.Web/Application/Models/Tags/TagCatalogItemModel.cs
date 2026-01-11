namespace PromptHub.Web.Application.Models.Tags;

public sealed record TagCatalogItemModel(
	string Tag,
	bool IsActive,
	string? DisplayName = null,
	int? SortOrder = null);
