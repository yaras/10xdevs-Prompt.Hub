// <copyright file="TablePromptReadStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Mapping;
using PromptHub.Web.Infrastructure.TableStorage.Pagination;
using PromptHub.Web.Infrastructure.TableStorage.Tables.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Tables.PublicPromptsNewestIndex;

namespace PromptHub.Web.Infrastructure.TableStorage.Stores;

/// <summary>
/// Azure Table Storage implementation of <see cref="IPromptReadStore" />.
/// </summary>
public sealed class TablePromptReadStore(
    PromptsTable promptsTable,
    PublicPromptsNewestIndexTable newestIndexTable) : IPromptReadStore
{
    /// <inheritdoc />
    public async Task<PromptModel?> GetByIdForAuthorAsync(string authorId, string promptId, CancellationToken ct)
    {
        var entity = await promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(authorId), promptId, ct);
        if (entity is null || entity.IsDeleted)
        {
            return null;
        }

        return PromptEntityMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<PromptModel?> GetPublicByIdAsync(string promptId, CancellationToken ct)
    {
        // MVP: public-by-id is fetched by scanning indexes would be expensive.
        // Implemented later when a direct public lookup strategy is decided.
        await Task.CompletedTask;
        return null;
    }

    /// <inheritdoc />
    public async Task<ContinuationPage<PromptSummaryModel>> ListMyPromptsAsync(
        string authorId,
        ContinuationToken? token,
        int pageSize,
        CancellationToken ct)
    {
        // MVP placeholder: implement when we add Table Storage query paging for user partition.
        await Task.CompletedTask;
        return new ContinuationPage<PromptSummaryModel>(Array.Empty<PromptSummaryModel>(), null);
    }

    /// <inheritdoc />
    public async Task<ContinuationPage<PromptSummaryModel>> ListPublicNewestAsync(
        ContinuationToken? token,
        int pageSize,
        CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var startingBucket = nowUtc.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);

        NewestPublicContinuationToken state = token is null
            ? new NewestPublicContinuationToken(startingBucket, null)
            : NewestPublicContinuationToken.Deserialize(token.Token);

        var bucket = state.Bucket;
        var pk = $"pub|newest|{bucket}";

        var (items, continuation) = await newestIndexTable.QueryPartitionAsync(pk, pageSize, state.Continuation, ct);

        var models = items
            .Select(static e => new PromptSummaryModel(
                PromptId: e.PromptId,
                AuthorId: e.AuthorId,
                Title: e.Title,
                Tags: TagString.ToTags(e.Tags),
                Visibility: PromptVisibility.Public,
                CreatedAt: e.CreatedAt,
                UpdatedAt: e.UpdatedAt,
                Likes: e.Likes,
                Dislikes: e.Dislikes))
            .ToArray();

        ContinuationToken? next = continuation is null
            ? null
            : new ContinuationToken(NewestPublicContinuationToken.Serialize(state with { Continuation = continuation }));

        return new ContinuationPage<PromptSummaryModel>(models, next);
    }
}
