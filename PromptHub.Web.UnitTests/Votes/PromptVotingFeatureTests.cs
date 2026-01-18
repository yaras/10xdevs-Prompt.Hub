// <copyright file="PromptVotingFeatureTests.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PromptHub.Web.Application.Abstractions.Persistence;
using PromptHub.Web.Application.Features.Votes;
using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.UnitTests.Votes;

#pragma warning disable CS1591
#pragma warning disable SA1600
public sealed class PromptVotingFeatureTests
{
    [Theory]
    [InlineData(VoteValue.None, VoteValue.Like, VoteValue.Like, 1, 0)]
    [InlineData(VoteValue.Like, VoteValue.Like, VoteValue.None, -1, 0)]
    [InlineData(VoteValue.None, VoteValue.Dislike, VoteValue.Dislike, 0, 1)]
    [InlineData(VoteValue.Dislike, VoteValue.Dislike, VoteValue.None, 0, -1)]
    [InlineData(VoteValue.Like, VoteValue.Dislike, VoteValue.Dislike, -1, 1)]
    [InlineData(VoteValue.Dislike, VoteValue.Like, VoteValue.Like, 1, -1)]
    public async Task VoteAsync_ComputesTransitionAndAppliesDeltas(
        VoteValue existing,
        VoteValue requested,
        VoteValue expectedNew,
        int expectedDeltaLikes,
        int expectedDeltaDislikes)
    {
        var voteStore = new FakeVoteStore(existing);
        var aggregates = new FakeAggregateStore();

        var sut = new PromptVotingFeature(voteStore, aggregates, NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: "a1", Requested: requested);
        var result = await sut.VoteAsync(request, voterId: "u1", CancellationToken.None);

        result.NewVote.Should().Be(expectedNew);
        voteStore.LastUpserted.Should().NotBeNull();
        voteStore.LastUpserted!.Value.NewVote.Should().Be(expectedNew);

        aggregates.LastCall.Should().NotBeNull();
        aggregates.LastCall!.Value.DeltaLikes.Should().Be(expectedDeltaLikes);
        aggregates.LastCall!.Value.DeltaDislikes.Should().Be(expectedDeltaDislikes);
    }

    [Fact]
    public async Task VoteAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var sut = new PromptVotingFeature(
            new FakeVoteStore(VoteValue.None),
            new FakeAggregateStore(),
            NullLogger<PromptVotingFeature>.Instance);

        var act = () => sut.VoteAsync(null!, voterId: "u1", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task VoteAsync_WhenPromptIdIsMissing_ThrowsArgumentException(string? promptId)
    {
        var sut = new PromptVotingFeature(
            new FakeVoteStore(VoteValue.None),
            new FakeAggregateStore(),
            NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: promptId!, AuthorId: "a1", Requested: VoteValue.Like);

        var act = () => sut.VoteAsync(request, voterId: "u1", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task VoteAsync_WhenAuthorIdIsMissing_ThrowsArgumentException(string? authorId)
    {
        var sut = new PromptVotingFeature(
            new FakeVoteStore(VoteValue.None),
            new FakeAggregateStore(),
            NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: authorId!, Requested: VoteValue.Like);

        var act = () => sut.VoteAsync(request, voterId: "u1", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task VoteAsync_WhenVoterIdIsMissing_ThrowsArgumentException(string? voterId)
    {
        var sut = new PromptVotingFeature(
            new FakeVoteStore(VoteValue.None),
            new FakeAggregateStore(),
            NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: "a1", Requested: VoteValue.Like);

        var act = () => sut.VoteAsync(request, voterId: voterId!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("voterId");
    }

    [Fact]
    public async Task VoteAsync_WhenRequestedVoteIsNotLikeOrDislike_ThrowsArgumentException()
    {
        var sut = new PromptVotingFeature(
            new FakeVoteStore(VoteValue.None),
            new FakeAggregateStore(),
            NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: "a1", Requested: VoteValue.None);

        var act = () => sut.VoteAsync(request, voterId: "u1", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task VoteAsync_ReturnsAggregatesFromStore()
    {
        var voteStore = new FakeVoteStore(VoteValue.None);
        var aggregates = new FakeAggregateStore(returnLikes: 10, returnDislikes: 3);

        var sut = new PromptVotingFeature(voteStore, aggregates, NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: "a1", Requested: VoteValue.Like);
        var result = await sut.VoteAsync(request, voterId: "u1", CancellationToken.None);

        result.Likes.Should().Be(10);
        result.Dislikes.Should().Be(3);
        result.NewVote.Should().Be(VoteValue.Like);
    }

    [Fact]
    public async Task VoteAsync_WhenUpsertThrows_PropagatesExceptionAndDoesNotUpdateAggregates()
    {
        var voteStore = new ThrowingVoteStore();
        var aggregates = new FakeAggregateStore();

        var sut = new PromptVotingFeature(voteStore, aggregates, NullLogger<PromptVotingFeature>.Instance);

        var request = new VoteRequest(PromptId: "p1", AuthorId: "a1", Requested: VoteValue.Like);

        var act = () => sut.VoteAsync(request, voterId: "u1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        aggregates.LastCall.Should().BeNull();
    }

#pragma warning restore SA1600
#pragma warning restore CS1591

    private sealed class FakeVoteStore(VoteValue existing) : IVoteStore
    {
        public (string PromptId, string VoterId, VoteValue NewVote)? LastUpserted { get; private set; }

        public Task<VoteStateModel?> GetVoteAsync(string promptId, string voterId, CancellationToken ct)
        {
            if (existing == VoteValue.None)
            {
                return Task.FromResult<VoteStateModel?>(null);
            }

            return Task.FromResult<VoteStateModel?>(new VoteStateModel(promptId, voterId, existing, DateTimeOffset.UtcNow));
        }

        public Task<VoteStateModel> UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct)
        {
            this.LastUpserted = (promptId, voterId, voteValue);
            return Task.FromResult(new VoteStateModel(promptId, voterId, voteValue, DateTimeOffset.UtcNow));
        }
    }

    private sealed class FakeAggregateStore(int returnLikes = 0, int returnDislikes = 0) : IPromptVoteAggregateStore
    {
        public (string AuthorId, string PromptId, int DeltaLikes, int DeltaDislikes)? LastCall { get; private set; }

        public Task<PromptAggregatesModel> ApplyVoteDeltaAsync(
            string authorId,
            string promptId,
            int deltaLikes,
            int deltaDislikes,
            CancellationToken ct)
        {
            this.LastCall = (authorId, promptId, deltaLikes, deltaDislikes);
            return Task.FromResult(new PromptAggregatesModel(returnLikes, returnDislikes));
        }
    }

    private sealed class ThrowingVoteStore : IVoteStore
    {
        public Task<VoteStateModel?> GetVoteAsync(string promptId, string voterId, CancellationToken ct)
            => Task.FromResult<VoteStateModel?>(null);

        public Task<VoteStateModel> UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct)
            => throw new InvalidOperationException("Store failure");
    }
}
