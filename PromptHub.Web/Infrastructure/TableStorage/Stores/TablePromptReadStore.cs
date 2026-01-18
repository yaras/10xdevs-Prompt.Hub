// <copyright file="TablePromptReadStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Azure.Data.Tables;
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
        if (string.IsNullOrWhiteSpace(promptId))
        {
            return null;
        }

        // MVP implementation:
        // - We do NOT have a dedicated PromptId -> (PartitionKey, RowKey) lookup for public prompts.
        // - The public-newest index is partitioned by month, so we search a bounded number
        //   of recent buckets for the prompt id.
        // - Once we have the author id from the index row, we can fetch the canonical prompt row
        //   directly (u|authorId + promptId) without scanning the Prompts table.
        const int maxBucketsToScan = 24;
        const int indexPageSize = 500;

        var nowUtc = DateTimeOffset.UtcNow;

        for (var i = 0; i < maxBucketsToScan; i++)
        {
            var bucketDate = nowUtc.AddMonths(-i);
            var pk = KeyFormat.PublicNewestPartitionKey(bucketDate);

            string? continuation = null;
            do
            {
                var (items, next) = await newestIndexTable.QueryPartitionAsync(pk, indexPageSize, continuation, ct);
                var match = items.FirstOrDefault(x => string.Equals(x.PromptId, promptId, StringComparison.Ordinal));
                if (match is not null)
                {
                    var entity = await promptsTable.GetAsync(KeyFormat.PromptsPartitionKey(match.AuthorId), promptId, ct);
                    if (entity is null || entity.IsDeleted)
                    {
                        return null;
                    }

                    var model = PromptEntityMapper.ToModel(entity);
                    return model.Visibility == PromptVisibility.Public ? model : null;
                }

                continuation = next;
            }
            while (!string.IsNullOrWhiteSpace(continuation));
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ContinuationPage<PromptSummaryModel>> ListMyPromptsAsync(
        string authorId,
        ContinuationToken? token,
        int pageSize,
        CancellationToken ct)
    {
        if (pageSize <= 0)
        {
            return new ContinuationPage<PromptSummaryModel>(Array.Empty<PromptSummaryModel>(), null);
        }

        var pk = KeyFormat.PromptsPartitionKey(authorId);

        var results = new List<PromptSummaryModel>(pageSize);

        var pages = promptsTable
            .QueryPartitionAsync(pk, maxPerPage: pageSize, cancellationToken: ct)
            .AsPages(continuationToken: token?.Token, pageSizeHint: pageSize);

        await foreach (var page in pages.WithCancellation(ct))
        {
            foreach (var entity in page.Values)
            {
                if (entity.IsDeleted)
                {
                    continue;
                }

                results.Add(new PromptSummaryModel(
                    PromptId: entity.PromptId,
                    AuthorId: entity.AuthorId,
                    AuthorEmail: entity.AuthorEmail,
                    Title: entity.Title,
                    Tags: TagString.ToTags(entity.Tags),
                    Visibility: PromptVisibilityMapper.ToModel(entity.Visibility),
                    CreatedAt: entity.CreatedAt,
                    UpdatedAt: entity.UpdatedAt,
                    Likes: entity.Likes,
                    Dislikes: entity.Dislikes,
                    UserVote: Application.Models.Votes.VoteValue.None));
            }

            ContinuationToken? next = string.IsNullOrWhiteSpace(page.ContinuationToken)
                ? null
                : new ContinuationToken(page.ContinuationToken);

            return new ContinuationPage<PromptSummaryModel>(results, next);
        }

        return new ContinuationPage<PromptSummaryModel>(results, null);
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
                AuthorEmail: e.AuthorEmail,
                Title: e.Title,
                Tags: TagString.ToTags(e.Tags),
                Visibility: PromptVisibility.Public,
                CreatedAt: e.CreatedAt,
                UpdatedAt: e.UpdatedAt,
                Likes: e.Likes,
                Dislikes: e.Dislikes,
                UserVote: Application.Models.Votes.VoteValue.None))
            .ToArray();

        ContinuationToken? next = continuation is null
            ? null
            : new ContinuationToken(NewestPublicContinuationToken.Serialize(state with { Continuation = continuation }));

        return new ContinuationPage<PromptSummaryModel>(models, next);
    }
}
