using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

public static class PromptVisibilityMapper
{
	public static string ToStorage(PromptVisibility visibility) => visibility switch
	{
		PromptVisibility.Public => "public",
		_ => "private",
	};

	public static PromptVisibility ToModel(string? visibility) => visibility?.Trim().ToLowerInvariant() switch
	{
		"public" => PromptVisibility.Public,
		_ => PromptVisibility.Private,
	};
}
