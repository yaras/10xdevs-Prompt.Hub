using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

public sealed class PromptEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PromptId { get; set; } = string.Empty;
	public string AuthorId { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string TitleNormalized { get; set; } = string.Empty;
	public string PromptText { get; set; } = string.Empty;
	public string Tags { get; set; } = string.Empty;
	public string Visibility { get; set; } = "private";
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	public bool IsDeleted { get; set; }
	public int Likes { get; set; }
	public int Dislikes { get; set; }
}
