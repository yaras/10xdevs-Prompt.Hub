// <copyright file="TableVoteStore.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Models.Votes;
using PromptHub.Web.Infrastructure.TableStorage.Mapping;
using PromptHub.Web.Infrastructure.TableStorage.Tables.PromptVotes;

namespace PromptHub.Web.Infrastructure.TableStorage.Stores;

/// <summary>
/// Azure Table Storage implementation of <see cref="IVoteStore" />.
/// </summary>
public sealed class TableVoteStore(PromptVotesTable table) : IVoteStore
{
    /// <inheritdoc />
    public async Task<VoteStateModel?> GetVoteAsync(string promptId, string voterId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(promptId) || string.IsNullOrWhiteSpace(voterId))
        {
            return null;
        }

        var entity = await table.GetAsync(KeyFormat.PromptVotesPartitionKey(promptId), KeyFormat.PromptVotesRowKey(voterId), ct);
        if (entity is null)
        {
            return null;
        }

        return new VoteStateModel(
            PromptId: entity.PromptId,
            VoterId: entity.VoterId,
            VoteValue: (VoteValue)entity.VoteValue,
            UpdatedAt: entity.UpdatedAt,
            ETag: entity.ETag.ToString());
    }

    /// <inheritdoc />
    public async Task<VoteStateModel> UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var entity = new Infrastructure.TableStorage.Entities.PromptVoteEntity
        {
            PartitionKey = KeyFormat.PromptVotesPartitionKey(promptId),
            RowKey = KeyFormat.PromptVotesRowKey(voterId),
            PromptId = promptId,
            VoterId = voterId,
            VoteValue = (int)voteValue,
            UpdatedAt = now,
        };

        await table.UpsertAsync(entity, ct);

        return new VoteStateModel(promptId, voterId, voteValue, now, entity.ETag.ToString());
    }
}
