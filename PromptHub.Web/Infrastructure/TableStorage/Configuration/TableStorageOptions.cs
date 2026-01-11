namespace PromptHub.Web.Infrastructure.TableStorage.Configuration;

public sealed class TableStorageOptions
{
	public const string SectionName = "TableStorage";

	public string ConnectionString { get; init; } = string.Empty;

	public string PromptsTableName { get; init; } = "Prompts";
	public string PromptVotesTableName { get; init; } = "PromptVotes";
	public string TagIndexTableName { get; init; } = "TagIndex";
	public string PublicPromptsNewestIndexTableName { get; init; } = "PublicPromptsNewestIndex";
	public string PublicPromptsMostLikedIndexTableName { get; init; } = "PublicPromptsMostLikedIndex";
	public string TitleSearchIndexTableName { get; init; } = "TitleSearchIndex";
	public string TagCatalogTableName { get; init; } = "TagCatalog";
}
