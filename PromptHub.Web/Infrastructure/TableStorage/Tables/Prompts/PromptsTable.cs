using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;

public sealed class PromptsTable
{
	private readonly TableClient table;

	public PromptsTable(ITableServiceClientFactory factory, IOptions<TableStorageOptions> options)
	{
		var serviceClient = factory.Create();
		table = serviceClient.GetTableClient(options.Value.PromptsTableName);
	}

	public Task CreateIfNotExistsAsync(CancellationToken ct) => table.CreateIfNotExistsAsync(ct);

	public async Task<PromptEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken ct)
	{
		try
		{
			var response = await table.GetEntityAsync<PromptEntity>(partitionKey, rowKey, cancellationToken: ct);
			return response.Value;
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public Task AddAsync(PromptEntity entity, CancellationToken ct) => table.AddEntityAsync(entity, ct);

	public Task UpsertAsync(PromptEntity entity, CancellationToken ct) => table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

	public Task UpdateAsync(PromptEntity entity, ETag etag, CancellationToken ct) =>
		table.UpdateEntityAsync(entity, etag, TableUpdateMode.Replace, ct);
}
