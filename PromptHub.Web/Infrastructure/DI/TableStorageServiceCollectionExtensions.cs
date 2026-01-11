using Microsoft.Extensions.Options;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Infrastructure.TableStorage.Client;
using PromptHub.Web.Infrastructure.TableStorage.Configuration;
using PromptHub.Web.Infrastructure.TableStorage.Stores;
using PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

namespace PromptHub.Web.Infrastructure.DI;

public static class TableStorageServiceCollectionExtensions
{
	public static IServiceCollection AddTableStorage(this IServiceCollection services, IConfiguration configuration)
	{
		services
			.AddOptions<TableStorageOptions>()
			.Bind(configuration.GetSection(TableStorageOptions.SectionName))
			.Validate(static o => !string.IsNullOrWhiteSpace(o.ConnectionString), "TableStorage connection string is required")
			.ValidateOnStart();

		services.AddSingleton<ITableServiceClientFactory, TableServiceClientFactory>();

		services.AddScoped<PromptsTable>();
		services.AddScoped<PublicPromptsNewestIndexTable>();

		services.AddScoped<IPromptReadStore, TablePromptReadStore>();

		return services;
	}
}
