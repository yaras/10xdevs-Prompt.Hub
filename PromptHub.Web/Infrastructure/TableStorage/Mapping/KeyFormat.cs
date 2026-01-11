namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

/// <summary>
/// Provides key formatting helpers for Azure Table Storage.
/// </summary>
public static class KeyFormat
{
    /// <summary>
    /// Builds the prompts partition key for a user.
    /// </summary>
    /// <param name="authorId">The author id.</param>
    /// <returns>The partition key.</returns>
    public static string PromptsPartitionKey(string authorId) => $"u|{authorId}";

    /// <summary>
    /// Builds the prompt votes partition key for a prompt.
    /// </summary>
    /// <param name="promptId">The prompt id.</param>
    /// <returns>The partition key.</returns>
    public static string PromptVotesPartitionKey(string promptId) => $"p|{promptId}";

    /// <summary>
    /// Builds the prompt votes row key for a voter.
    /// </summary>
    /// <param name="voterId">The voter id.</param>
    /// <returns>The row key.</returns>
    public static string PromptVotesRowKey(string voterId) => $"u|{voterId}";

    /// <summary>
    /// Builds the tag index partition key for a tag.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The partition key.</returns>
    public static string TagIndexPartitionKey(string tag) => $"t|{tag.Trim().ToLowerInvariant()}";

    /// <summary>
    /// Builds the tag catalog partition key.
    /// </summary>
    /// <returns>The partition key.</returns>
    public static string TagCatalogPartitionKey() => "tagcatalog";

    /// <summary>
    /// Builds the partition key for the public newest index.
    /// </summary>
    /// <param name="createdAtUtc">The creation timestamp (UTC).</param>
    /// <returns>The partition key.</returns>
    public static string PublicNewestPartitionKey(DateTimeOffset createdAtUtc) => $"pub|newest|{createdAtUtc:yyyyMM}";

    /// <summary>
    /// Computes the row key prefix for descending created-at ordering.
    /// </summary>
    /// <param name="createdAtUtc">The creation timestamp (UTC).</param>
    /// <returns>A zero-padded, lexicographically sortable string.</returns>
    public static string CreatedAtTicksDesc(DateTimeOffset createdAtUtc)
    {
        var desc = DateTimeOffset.MaxValue.UtcTicks - createdAtUtc.UtcTicks;
        return desc.ToString("D19", System.Globalization.CultureInfo.InvariantCulture);
    }
}
