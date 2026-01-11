using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

public sealed class TitleSearchIndexEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Token { get; set; } = string.Empty;
	public string PromptId { get; set; } = string.Empty;
	public string Visibility { get; set; } = "private";
	public string AuthorId { get; set; } = string.Empty;
	public bool IsDeleted { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
}
