using System.Text.Json;

namespace PromptHub.Web.Infrastructure.TableStorage.Pagination;

public sealed record NewestPublicContinuationToken(string Bucket, string? Continuation)
{
	public static string Serialize(NewestPublicContinuationToken token) => JsonSerializer.Serialize(token);
	public static NewestPublicContinuationToken Deserialize(string value) =>
		JsonSerializer.Deserialize<NewestPublicContinuationToken>(value)
		?? throw new InvalidOperationException("Invalid continuation token.");
}
