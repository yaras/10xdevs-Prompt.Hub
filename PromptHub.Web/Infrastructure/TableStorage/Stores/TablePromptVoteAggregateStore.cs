// <copyright file="TablePromptVoteAggregateStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Infrastructure.TableStorage.Mapping;
using PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

namespace PromptHub.Web.Infrastructure.TableStorage.Stores;

/// <summary>
/// Updates prompt like/dislike aggregates in Azure Table Storage with optimistic concurrency.
/// </summary>
public sealed class TablePromptVoteAggregateStore(
    PromptsTable promptsTable,
    PublicPromptsNewestIndexTable publicNewestIndexTable) : IPromptVoteAggregateStore
{
    private const int MaxAttempts = 5;

    /// <inheritdoc />
    public async Task<PromptAggregatesModel> ApplyVoteDeltaAsync(
        string authorId,
        string promptId,
        int deltaLikes,
        int deltaDislikes,
        CancellationToken ct)
    {
        if (deltaLikes == 0 && deltaDislikes == 0)
        {
            var current = await promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(authorId), promptId, ct)
                ?? throw new InvalidOperationException("Prompt not found.");

            return new PromptAggregatesModel(current.Likes, current.Dislikes);
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var entity = await promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(authorId), promptId, ct)
                ?? throw new InvalidOperationException("Prompt not found.");

            if (entity.IsDeleted)
            {
                throw new InvalidOperationException("Prompt not found.");
            }

            var nextLikes = Math.Max(0, entity.Likes + deltaLikes);
            var nextDislikes = Math.Max(0, entity.Dislikes + deltaDislikes);

            entity.Likes = nextLikes;
            entity.Dislikes = nextDislikes;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await promptsTable.UpdateAsync(entity, entity.ETag, ct);

                // Best-effort: update the public newest index denormalized fields.
                // The index key is derived from CreatedAt and PromptId.
                if (PromptVisibilityMapper.ToModel(entity.Visibility) == Application.Models.Prompts.PromptVisibility.Public)
                {
                    var pk = KeyFormat.PublicNewestPartitionKey(entity.CreatedAt);
                    var rk = $"{KeyFormat.CreatedAtTicksDesc(entity.CreatedAt)}|{entity.PromptId}";

                    var indexEntity = new Infrastructure.TableStorage.Entities.PublicPromptsNewestIndexEntity
                    {
                        PartitionKey = pk,
                        RowKey = rk,
                        PromptId = entity.PromptId,
                        AuthorId = entity.AuthorId,
                        AuthorEmail = entity.AuthorEmail,
                        Title = entity.Title,
                        TitleNormalized = entity.TitleNormalized,
                        Tags = entity.Tags,
                        CreatedAt = entity.CreatedAt,
                        UpdatedAt = entity.UpdatedAt,
                        Likes = nextLikes,
                        Dislikes = nextDislikes,
                    };

                    await publicNewestIndexTable.UpsertAsync(indexEntity, ct);
                }

                return new PromptAggregatesModel(nextLikes, nextDislikes);
            }
            catch (RequestFailedException ex) when (ex.Status == 412 || ex.Status == 409)
            {
                if (attempt == MaxAttempts)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), ct);
            }
        }

        throw new InvalidOperationException("Failed to update aggregates.");
    }
}
