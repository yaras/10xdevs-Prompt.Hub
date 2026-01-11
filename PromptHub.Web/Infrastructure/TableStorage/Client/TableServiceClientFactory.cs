using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;

namespace PromptHub.Web.Infrastructure.TableStorage.Client;

public sealed class TableServiceClientFactory(IOptions<TableStorageOptions> options) : ITableServiceClientFactory
{
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
