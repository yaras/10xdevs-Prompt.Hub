using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Application.Abstractions.Persistence;

public interface IPromptReadStore
{
	Task<PromptModel?> GetByIdForAuthorAsync(string authorId, string promptId, CancellationToken ct);
	Task<PromptModel?> GetPublicByIdAsync(string promptId, CancellationToken ct);
	Task<ContinuationPage<PromptSummaryModel>> ListMyPromptsAsync(string authorId, ContinuationToken? token, int pageSize, CancellationToken ct);
	Task<ContinuationPage<PromptSummaryModel>> ListPublicNewestAsync(ContinuationToken? token, int pageSize, CancellationToken ct);
}
