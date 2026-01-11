using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;

/// <summary>
/// Provides access to the <c>Prompts</c> Azure Table.
/// </summary>
public sealed class PromptsTable
{
    private readonly TableClient table;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptsTable" /> class.
    /// </summary>
    /// <param name="factory">The table service client factory.</param>
    /// <param name="options">The storage options.</param>
    public PromptsTable(ITableServiceClientFactory factory, IOptions<TableStorageOptions> options)
    {
        var serviceClient = factory.Create();
        table = serviceClient.GetTableClient(options.Value.PromptsTableName);
    }

    /// <summary>
    /// Ensures the backing table exists.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CreateIfNotExistsAsync(CancellationToken ct) => table.CreateIfNotExistsAsync(ct);

    /// <summary>
    /// Gets a prompt entity by partition key and row key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The entity if found; otherwise <see langword="null" />.</returns>
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

    /// <summary>
    /// Adds an entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddAsync(PromptEntity entity, CancellationToken ct) => table.AddEntityAsync(entity, ct);

    /// <summary>
    /// Upserts an entity.
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpsertAsync(PromptEntity entity, CancellationToken ct) => table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

    /// <summary>
    /// Updates an entity using optimistic concurrency.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="etag">The expected ETag.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateAsync(PromptEntity entity, ETag etag, CancellationToken ct) =>
        table.UpdateEntityAsync(entity, etag, TableUpdateMode.Replace, ct);
}
