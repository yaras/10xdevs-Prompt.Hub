namespace PromptHub.Web.Application.Abstractions.Persistence;

/// <summary>
/// Represents an opaque continuation token used for pagination.
/// </summary>
/// <param name="Value">The serialized token value.</param>
public sealed record ContinuationToken(string Value);
