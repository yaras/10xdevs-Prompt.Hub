namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

public static class TagString
{
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
