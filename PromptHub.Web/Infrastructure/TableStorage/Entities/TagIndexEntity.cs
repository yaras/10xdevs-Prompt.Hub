using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

public sealed class TagIndexEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Tag { get; set; } = string.Empty;
	public string PromptId { get; set; } = string.Empty;
	public string AuthorId { get; set; } = string.Empty;
	public string Visibility { get; set; } = "private";
	public DateTimeOffset CreatedAt { get; set; }
	public int Likes { get; set; }
	public int Dislikes { get; set; }
	public bool IsDeleted { get; set; }
}
