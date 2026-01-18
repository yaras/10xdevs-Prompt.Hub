// <copyright file="OpenAiTagSuggestionServiceTests.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI;
using PromptHub.Web.Application.TagSuggestions;
using PromptHub.Web.Infrastructure.OpenAI;

namespace PromptHub.Web.UnitTests.OpenAI;

#pragma warning disable CS1591
#pragma warning disable SA1600

public sealed class OpenAiTagSuggestionServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task SuggestTagsAsync_WhenTitleIsMissing_ThrowsArgumentException(string? title)
    {
        var service = new OpenAiTagSuggestionService(
            client: new OpenAIClient("test"),
            options: Options.Create(new TagSuggestionOptions { AllowedTags = ["csharp"], Model = "gpt-4o-mini" }),
            logger: NullLogger<OpenAiTagSuggestionService>.Instance);

        var act = () => service.SuggestTagsAsync(title!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public async Task SuggestTagsAsync_WhenAllowedTagsEmpty_ReturnsEmpty()
    {
        var service = new OpenAiTagSuggestionService(
            client: new OpenAIClient("test"),
            options: Options.Create(new TagSuggestionOptions { AllowedTags = [], Model = "gpt-4o-mini" }),
            logger: NullLogger<OpenAiTagSuggestionService>.Instance);

        var result = await service.SuggestTagsAsync("some title", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestTagsAsync_WhenAllowedTagsAreWhitespace_ReturnsEmpty()
    {
        var service = new OpenAiTagSuggestionService(
            client: new OpenAIClient("test"),
            options: Options.Create(new TagSuggestionOptions { AllowedTags = ["  ", "\t"], Model = "gpt-4o-mini" }),
            logger: NullLogger<OpenAiTagSuggestionService>.Instance);

        var result = await service.SuggestTagsAsync("some title", CancellationToken.None);
        result.Should().BeEmpty();
    }
}
