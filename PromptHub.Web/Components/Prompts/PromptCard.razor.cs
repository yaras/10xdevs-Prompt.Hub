// <copyright file="PromptCard.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Application.Models.Votes;

namespace PromptHub.Web.Components.Prompts
{
    /// <summary>
    /// Renders a prompt summary as a card suitable for list/grid views.
    /// </summary>
    public partial class PromptCard
    {
        /// <summary>
        /// Gets or sets the prompt data to render.
        /// </summary>
        [Parameter, EditorRequired]
        public PromptSummaryModel Prompt { get; set; } = default!;

        [Parameter]
        public string? CurrentUserAuthorId { get; set; }

        public bool IsOwn =>
            !string.IsNullOrWhiteSpace(this.CurrentUserAuthorId)
            && !string.IsNullOrWhiteSpace(this.Prompt.AuthorId)
            && string.Equals(this.CurrentUserAuthorId, this.Prompt.AuthorId, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets a value indicating whether the edit action is displayed.
        /// </summary>
        [Parameter]
        public bool ShowEdit { get; set; }

        [Parameter]
        public bool ShowVoting { get; set; }

        [Parameter]
        public EventCallback<VoteRequest> OnVote { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the edit action is clicked.
        /// The callback parameter is the <see cref="PromptSummaryModel.PromptId"/>.
        /// </summary>
        [Parameter]
        public EventCallback<string> OnEdit { get; set; }

        [Parameter]
        public EventCallback<string> OnView { get; set; }

        private Task LikeClicked() => this.RaiseVoteAsync(VoteValue.Like);

        private Task DislikeClicked() => this.RaiseVoteAsync(VoteValue.Dislike);

        private Task RaiseVoteAsync(VoteValue requested)
        {
            if (!this.ShowVoting || !this.OnVote.HasDelegate)
            {
                return Task.CompletedTask;
            }

            return this.OnVote.InvokeAsync(new VoteRequest(
                PromptId: this.Prompt.PromptId,
                AuthorId: this.Prompt.AuthorId,
                Requested: requested));
        }

        private Task EditClicked()
        {
            if (!this.OnEdit.HasDelegate)
            {
                return Task.CompletedTask;
            }

            return this.OnEdit.InvokeAsync(this.Prompt.PromptId);
        }

        private Task ViewClicked()
        {
            if (!this.OnView.HasDelegate)
            {
                return Task.CompletedTask;
            }

            return this.OnView.InvokeAsync(this.Prompt.PromptId);
        }
    }
}