using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

/// <summary>
/// Provides access to the <c>PublicPromptsNewestIndex</c> Azure Table.
/// </summary>
public sealed class PublicPromptsNewestIndexTable
{
    private readonly TableClient table;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicPromptsNewestIndexTable" /> class.
    /// </summary>
    /// <param name="factory">The table service client factory.</param>
    /// <param name="options">The storage options.</param>
    public PublicPromptsNewestIndexTable(ITableServiceClientFactory factory, IOptions<TableStorageOptions> options)
    {
        var serviceClient = factory.Create();
        table = serviceClient.GetTableClient(options.Value.PublicPromptsNewestIndexTableName);
    }

    /// <summary>
    /// Ensures the backing table exists.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CreateIfNotExistsAsync(CancellationToken ct) => table.CreateIfNotExistsAsync(ct);

    /// <summary>
    /// Upserts an entity.
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpsertAsync(PublicPromptsNewestIndexEntity entity, CancellationToken ct) =>
        table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

    /// <summary>
    /// Deletes an entity by partition key and row key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct) =>
        table.DeleteEntityAsync(partitionKey, rowKey, ETag.All, ct);

    /// <summary>
    /// Queries a single partition.
    /// </summary>
    /// <param name="partitionKey">The partition key to query.</param>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The items and an optional continuation token.</returns>
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
