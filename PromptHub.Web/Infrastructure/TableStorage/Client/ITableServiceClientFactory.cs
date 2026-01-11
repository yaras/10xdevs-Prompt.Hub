using Azure.Data.Tables;

namespace PromptHub.Web.Infrastructure.TableStorage.Client;

public interface ITableServiceClientFactory
{
	TableServiceClient Create();
}
