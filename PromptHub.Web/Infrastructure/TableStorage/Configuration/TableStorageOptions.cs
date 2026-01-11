namespace PromptHub.Web.Infrastructure.TableStorage.Configuration;

/// <summary>
/// Provides configuration options for Azure Table Storage.
/// </summary>
public sealed class TableStorageOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "TableStorage";

    /// <summary>
    /// Gets the storage account connection string.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets the table name for prompts.
    /// </summary>
    public string PromptsTableName { get; init; } = "Prompts";

    /// <summary>
    /// Gets the table name for prompt votes.
    /// </summary>
    public string PromptVotesTableName { get; init; } = "PromptVotes";

    /// <summary>
    /// Gets the table name for the tag index.
    /// </summary>
    public string TagIndexTableName { get; init; } = "TagIndex";

    /// <summary>
    /// Gets the table name for the public prompts newest index.
    /// </summary>
    public string PublicPromptsNewestIndexTableName { get; init; } = "PublicPromptsNewestIndex";

    /// <summary>
    /// Gets the table name for the public prompts most-liked index.
    /// </summary>
    public string PublicPromptsMostLikedIndexTableName { get; init; } = "PublicPromptsMostLikedIndex";

    /// <summary>
    /// Gets the table name for the title search index.
    /// </summary>
    public string TitleSearchIndexTableName { get; init; } = "TitleSearchIndex";

    /// <summary>
    /// Gets the table name for the tag catalog.
    /// </summary>
    public string TagCatalogTableName { get; init; } = "TagCatalog";
}
