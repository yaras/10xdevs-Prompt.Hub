// <copyright file="OpenAiTagSuggestionService.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using PromptHub.Web.Application.TagSuggestions;

namespace PromptHub.Web.Infrastructure.OpenAI;

/// <summary>
/// OpenAI implementation of <see cref="ITagSuggestionService"/>.
/// </summary>
public sealed class OpenAiTagSuggestionService : ITagSuggestionService
{
    private readonly OpenAIClient client;
    private readonly TagSuggestionOptions options;
    private readonly ILogger<OpenAiTagSuggestionService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiTagSuggestionService"/> class.
    /// </summary>
    /// <param name="client">OpenAI client.</param>
    /// <param name="options">Tag suggestion options.</param>
    /// <param name="logger">Logger.</param>
    public OpenAiTagSuggestionService(OpenAIClient client, IOptions<TagSuggestionOptions> options, ILogger<OpenAiTagSuggestionService> logger)
    {
        this.client = client;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SuggestTagsAsync(string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (this.options.AllowedTags.Length == 0)
        {
            return Array.Empty<string>();
        }

        var allowed = this.options.AllowedTags
            .Select(static t => t?.Trim().ToLowerInvariant())
            .Where(static t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (allowed.Length == 0)
        {
            return Array.Empty<string>();
        }

        var maxSuggestions = this.options.MaxSuggestions <= 0 ? 4 : this.options.MaxSuggestions;

        var system = "You are a tag suggestion engine. You only suggest tags from the allowed list. Output must be a JSON array of strings. No extra text.";
        var user = $"Title: {title}\nAllowed tags: [{string.Join(", ", allowed)}]\nReturn up to {maxSuggestions} tags.";

        try
        {
            var chat = this.client.GetChatClient(this.options.Model);

            ChatCompletion completion = await chat.CompleteChatAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage(system),
                    new UserChatMessage(user),
                },
                cancellationToken: cancellationToken);

            var content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty;

            var parsed = TryParseJsonStringArray(content, out var tags)
                ? tags
                : FallbackSplit(content);

            var result = parsed
                .Select(static t => t.Trim().ToLowerInvariant())
                .Where(static t => !string.IsNullOrWhiteSpace(t))
                .Where(t => allowed.Contains(t, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Take(maxSuggestions)
                .ToArray();

            return result;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to suggest tags using OpenAI.");
            throw;
        }
    }

    private static bool TryParseJsonStringArray(string content, out string[] tags)
    {
        tags = Array.Empty<string>();

        try
        {
            var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var list = new List<string>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    list.Add(item.GetString() ?? string.Empty);
                }
            }

            tags = list.ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> FallbackSplit(string content)
    {
        return content
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static t => t.Trim().Trim('"'));
    }
}
