using PromptHub.Web.Application.Models.Prompts;

namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

/// <summary>
/// Maps visibility values between the application model and the Table Storage representation.
/// </summary>
public static class PromptVisibilityMapper
{
    /// <summary>
    /// Converts an application visibility value to its storage representation.
    /// </summary>
    /// <param name="visibility">The visibility value.</param>
    /// <returns>The storage value.</returns>
    public static string ToStorage(PromptVisibility visibility) => visibility switch
    {
        PromptVisibility.Public => "public",
        _ => "private",
    };

    /// <summary>
    /// Converts a storage visibility value to an application visibility value.
    /// </summary>
    /// <param name="visibility">The storage value.</param>
    /// <returns>The application visibility value.</returns>
    public static PromptVisibility ToModel(string? visibility) => visibility?.Trim().ToLowerInvariant() switch
    {
        "public" => PromptVisibility.Public,
        _ => PromptVisibility.Private,
    };
}
