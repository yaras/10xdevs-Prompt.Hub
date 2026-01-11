using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

public sealed class PublicPromptsNewestIndexTable
{
	private readonly TableClient table;

	public PublicPromptsNewestIndexTable(ITableServiceClientFactory factory, IOptions<TableStorageOptions> options)
	{
		var serviceClient = factory.Create();
		table = serviceClient.GetTableClient(options.Value.PublicPromptsNewestIndexTableName);
	}

	public Task CreateIfNotExistsAsync(CancellationToken ct) => table.CreateIfNotExistsAsync(ct);

	public Task UpsertAsync(PublicPromptsNewestIndexEntity entity, CancellationToken ct) =>
		table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

	public Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct) =>
		table.DeleteEntityAsync(partitionKey, rowKey, ETag.All, ct);

	public async Task<(IReadOnlyList<PublicPromptsNewestIndexEntity> Items, string? Continuation)> QueryPartitionAsync(
		string partitionKey,
		int pageSize,
		string? continuationToken,
		CancellationToken ct)
	{
		var results = new List<PublicPromptsNewestIndexEntity>(pageSize);
		var pages = table
			.QueryAsync<PublicPromptsNewestIndexEntity>(x => x.PartitionKey == partitionKey, maxPerPage: pageSize, cancellationToken: ct)
			.AsPages(continuationToken, pageSize);

		await foreach (var page in pages.WithCancellation(ct))
		{
			results.AddRange(page.Values);
			return (results, page.ContinuationToken);
		}

		return (results, null);
	}
}
