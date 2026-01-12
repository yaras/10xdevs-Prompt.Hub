// <copyright file="PromptVotesTable.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Tables.PromptVotes;

/// <summary>
/// Provides access to the <c>PromptVotes</c> Azure Table.
/// </summary>
public sealed class PromptVotesTable
{
    private readonly TableClient table;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptVotesTable" /> class.
    /// </summary>
    /// <param name="factory">The table service client factory.</param>
    /// <param name="options">The storage options.</param>
    public PromptVotesTable(ITableServiceClientFactory factory, IOptions<TableStorageOptions> options)
    {
        var serviceClient = factory.Create();
        this.table = serviceClient.GetTableClient(options.Value.PromptVotesTableName);
    }

    /// <summary>
    /// Ensures the backing table exists.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public Task CreateIfNotExistsAsync(CancellationToken ct) => this.table.CreateIfNotExistsAsync(ct);

    /// <summary>
    /// Gets a vote entity.
    /// </summary>
    public async Task<PromptVoteEntity?> GetAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        try
        {
            var response = await this.table.GetEntityAsync<PromptVoteEntity>(partitionKey, rowKey, cancellationToken: ct);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Upserts an entity.
    /// </summary>
    public Task UpsertAsync(PromptVoteEntity entity, CancellationToken ct) =>
        this.table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
}
