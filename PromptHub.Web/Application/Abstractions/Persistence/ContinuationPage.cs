namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Represents a page of results along with an optional continuation token.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The page items.</param>
/// <param name="ContinuationToken">The continuation token for the next page, if any.</param>
public sealed record ContinuationPage<T>(IReadOnlyList<T> Items, ContinuationToken? ContinuationToken);
