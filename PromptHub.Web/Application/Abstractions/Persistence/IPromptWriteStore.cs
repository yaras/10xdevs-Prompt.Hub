using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Application.Abstractions.Persistence;

public interface IPromptWriteStore
{
	Task<PromptModel> CreateAsync(PromptModel prompt, CancellationToken ct);
	Task<PromptModel> UpdateAsync(PromptModel prompt, string expectedETag, CancellationToken ct);
	Task SoftDeleteAsync(string authorId, string promptId, CancellationToken ct);
}
