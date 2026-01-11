namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

public static class KeyFormat
{
	public static string PromptsPartitionKey(string authorId) => $"u|{authorId}";
	public static string PromptVotesPartitionKey(string promptId) => $"p|{promptId}";
	public static string PromptVotesRowKey(string voterId) => $"u|{voterId}";
	public static string TagIndexPartitionKey(string tag) => $"t|{tag.Trim().ToLowerInvariant()}";
	public static string TagCatalogPartitionKey() => "tagcatalog";

	public static string PublicNewestPartitionKey(DateTimeOffset createdAtUtc) => $"pub|newest|{createdAtUtc:yyyyMM}";

	public static string CreatedAtTicksDesc(DateTimeOffset createdAtUtc)
	{
		var desc = DateTimeOffset.MaxValue.UtcTicks - createdAtUtc.UtcTicks;
		return desc.ToString("D19", System.Globalization.CultureInfo.InvariantCulture);
	}
}
