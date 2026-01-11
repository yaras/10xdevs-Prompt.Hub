// <copyright file="ITableServiceClientFactory.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Client;

/// <summary>
/// Creates <see cref="TableServiceClient" /> instances.
/// </summary>
public interface ITableServiceClientFactory
{
    /// <summary>
    /// Creates a configured <see cref="TableServiceClient" />.
    /// </summary>
    /// <returns>The table service client.</returns>
    TableServiceClient Create();
}
