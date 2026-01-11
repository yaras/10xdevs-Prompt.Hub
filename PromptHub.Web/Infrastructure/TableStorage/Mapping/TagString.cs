namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

/// <summary>
/// Converts between tag lists and the delimited string representation stored in Table Storage.
/// </summary>
public static class TagString
{
    /// <summary>
    /// Parses a delimited tag string.
    /// </summary>
    /// <param name="value">The delimited string value.</param>
    /// <returns>A list of normalized tags.</returns>
    public static IReadOnlyList<string> ToTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static x => x.ToLowerInvariant())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Formats tags as a normalized delimited string.
    /// </summary>
    /// <param name="tags">The tags.</param>
    /// <returns>The delimited tag string.</returns>
    public static string ToDelimited(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(';', tags
            .Select(static t => t.Trim().ToLowerInvariant())
            .Where(static t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal));
    }
}
