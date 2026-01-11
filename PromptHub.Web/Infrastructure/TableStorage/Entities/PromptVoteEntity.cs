using Azure;
using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Entities;

public sealed class PromptVoteEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PromptId { get; set; } = string.Empty;
	public string VoterId { get; set; } = string.Empty;
	public int VoteValue { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
}
