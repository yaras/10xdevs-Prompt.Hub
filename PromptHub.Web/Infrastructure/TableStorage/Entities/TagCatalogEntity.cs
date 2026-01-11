using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

public sealed class TagCatalogEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Tag { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
	public string? DisplayName { get; set; }
	public int? SortOrder { get; set; }
}
