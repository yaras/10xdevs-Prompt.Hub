// <copyright file="TablePromptWriteStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Entities;
using PromptHub.Web.Infrastructure.TableStorage.Mapping;
using PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

namespace PromptHub.Web.Infrastructure.TableStorage.Stores;

/// <summary>
/// Provides write operations for prompts using Azure Table Storage.
/// </summary>
public sealed class TablePromptWriteStore : IPromptWriteStore
{
    private readonly PromptsTable promptsTable;
    private readonly PublicPromptsNewestIndexTable publicIndexTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="TablePromptWriteStore"/> class.
    /// </summary>
    /// <param name="promptsTable">The prompts table.</param>
    /// <param name="publicIndexTable">The public prompts index table.</param>
    public TablePromptWriteStore(PromptsTable promptsTable, PublicPromptsNewestIndexTable publicIndexTable)
    {
        this.promptsTable = promptsTable;
        this.publicIndexTable = publicIndexTable;
    }

    /// <inheritdoc />
    public async Task<PromptModel> CreateAsync(PromptModel prompt, CancellationToken ct)
    {
        var promptId = Guid.NewGuid().ToString("N");
        var newPrompt = prompt with { PromptId = promptId };

        var entity = PromptEntityMapper.FromModel(newPrompt);
        await this.promptsTable.AddAsync(entity, ct);

        var createdPrompt = newPrompt with { ETag = entity.ETag.ToString() };

        if (createdPrompt.Visibility == PromptVisibility.Public)
        {
            var indexEntity = ToPublicNewestIndexEntity(createdPrompt);
            await this.publicIndexTable.UpsertAsync(indexEntity, ct);
        }

        return createdPrompt;
    }

    /// <inheritdoc />
    public async Task<PromptModel> UpdateAsync(PromptModel prompt, string expectedETag, CancellationToken ct)
    {
        var existing = await this.promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(prompt.AuthorId), prompt.PromptId, ct);
        if (existing is null || existing.IsDeleted)
        {
            throw new InvalidOperationException("Prompt not found.");
        }

        var updatedEntity = PromptEntityMapper.FromModel(prompt);
        await this.promptsTable.UpdateAsync(updatedEntity, new ETag(expectedETag), ct);

        var updatedPrompt = prompt with { ETag = updatedEntity.ETag.ToString() };

        // MVP: only the newest public index is maintained.
        // If visibility changed, delete old index row and upsert new row (if public).
        if (PromptVisibilityMapper.ToModel(existing.Visibility) == PromptVisibility.Public)
        {
            await this.publicIndexTable.DeleteAsync(
                KeyFormat.PublicNewestPartitionKey(existing.CreatedAt),
                $"{KeyFormat.CreatedAtTicksDesc(existing.CreatedAt)}|{existing.PromptId}",
                ct);
        }

        if (updatedPrompt.Visibility == PromptVisibility.Public)
        {
            var indexEntity = ToPublicNewestIndexEntity(updatedPrompt);
            await this.publicIndexTable.UpsertAsync(indexEntity, ct);
        }

        return updatedPrompt;
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(string authorId, string promptId, CancellationToken ct)
    {
        var entity = await this.promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(authorId), promptId, ct);
        if (entity is null)
        {
            return;
        }

        entity.IsDeleted = true;
        await this.promptsTable.UpdateAsync(entity, entity.ETag, ct);

        if (PromptVisibilityMapper.ToModel(entity.Visibility) == PromptVisibility.Public)
        {
            await this.publicIndexTable.DeleteAsync(
                KeyFormat.PublicNewestPartitionKey(entity.CreatedAt),
                $"{KeyFormat.CreatedAtTicksDesc(entity.CreatedAt)}|{entity.PromptId}",
                ct);
        }
    }

    private static PublicPromptsNewestIndexEntity ToPublicNewestIndexEntity(PromptModel model)
    {
        var createdAtUtc = model.CreatedAt == default ? DateTimeOffset.UtcNow : model.CreatedAt;
        var rowKey = $"{KeyFormat.CreatedAtTicksDesc(createdAtUtc)}|{model.PromptId}";
        var title = model.Title.Trim();

        return new PublicPromptsNewestIndexEntity
        {
            PartitionKey = KeyFormat.PublicNewestPartitionKey(createdAtUtc),
            RowKey = rowKey,
            PromptId = model.PromptId,
            AuthorId = model.AuthorId,
            AuthorEmail = model.AuthorEmail,
            Title = title,
            TitleNormalized = title.ToLowerInvariant(),
            Tags = TagString.ToDelimited(model.Tags),
            CreatedAt = createdAtUtc,
            UpdatedAt = model.UpdatedAt == default ? createdAtUtc : model.UpdatedAt,
            Likes = model.Likes,
            Dislikes = model.Dislikes,
        };
    }
}