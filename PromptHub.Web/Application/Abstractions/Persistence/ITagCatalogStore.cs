using PromptHub.Web.Application.Models.Tags;

namespace PromptHub.Web.Application.Abstractions.Persistence;

public interface ITagCatalogStore
{
	Task<IReadOnlyList<TagCatalogItemModel>> GetActiveTagsAsync(CancellationToken ct);
}
