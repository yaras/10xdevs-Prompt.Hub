// <copyright file="ContinuationToken.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Represents a continuation token for paginated queries.
/// </summary>
public sealed record ContinuationToken(string Token);
