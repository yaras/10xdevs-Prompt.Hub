// <copyright file="TableServiceClientFactory.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;

namespace PromptHub.Web.Infrastructure.TableStorage.Client;

/// <summary>
/// Default implementation of <see cref="ITableServiceClientFactory" />.
/// </summary>
public sealed class TableServiceClientFactory(IOptions<TableStorageOptions> options) : ITableServiceClientFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="TableServiceClient" />.
    /// </summary>
    /// <returns>A new instance of <see cref="TableServiceClient" />.</returns>
    /// <inheritdoc />
    public TableServiceClient Create()
    {
        var connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"'{TableStorageOptions.SectionName}:ConnectionString' is not configured.");
        }

        return new TableServiceClient(connectionString);
    }
}
