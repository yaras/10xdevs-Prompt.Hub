// <copyright file="OpenAiTagSuggestionService.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using System.Text.Json;
using Json.Schema;
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
    private static readonly JsonSchema ResponseSchema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(
            ("tags", new JsonSchemaBuilder()
                .Type(SchemaValueType.Array)
                .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))))
        .Required("tags")
        .AdditionalProperties(false);

    private static readonly string ResponseSchemaJson = JsonSerializer.Serialize(
        ResponseSchema,
        new JsonSerializerOptions
        {
            WriteIndented = false,
        });

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

        var system = "You are a tag suggestion engine. You only suggest tags from the allowed list. Output must be valid JSON matching the provided schema. No extra text.";
        var user = $"Title: {title}\nAllowed tags: [{string.Join(", ", allowed)}]\nReturn up to {maxSuggestions} tags in JSON format";

        try
        {
            var chat = this.client.GetChatClient(this.options.Model);

            var chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "tags_result",
                jsonSchema: BinaryData.FromString(ResponseSchemaJson),
                jsonSchemaIsStrict: true);

            var options = new ChatCompletionOptions
            {
                ResponseFormat = chatResponseFormat,
            };

            ChatCompletion completion = await chat.CompleteChatAsync(
                [
                    new SystemChatMessage(system),
                    new UserChatMessage(user),
                ],
                options: options,
                cancellationToken: cancellationToken);

            var content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty;

            if (!TryParseAndValidateResponse(content, out var tags))
            {
                throw new InvalidOperationException("OpenAI response did not match the expected JSON schema.");
            }

            var result = tags
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

    private static bool TryParseAndValidateResponse(string content, out string[] tags)
    {
        tags = Array.Empty<string>();

        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(content);
            var evaluation = ResponseSchema.Evaluate(element);
            if (!evaluation.IsValid)
            {
                return false;
            }

            if (!element.TryGetProperty("tags", out var tagsElement) || tagsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var list = new List<string>();
            foreach (var item in tagsElement.EnumerateArray())
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
}
