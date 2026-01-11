namespace PromptHub.Web.Application.Abstractions.Persistence;

public sealed record ContinuationPage<T>(IReadOnlyList<T> Items, ContinuationToken? ContinuationToken);
