using PromptHub.Web.Application.Models.Prompts;
using PromptHub.Web.Infrastructure.TableStorage.Entities;

namespace PromptHub.Web.Infrastructure.TableStorage.Mapping;

public static class PromptEntityMapper
{
	public static PromptModel ToModel(PromptEntity entity) => new(
		PromptId: entity.PromptId,
		AuthorId: entity.AuthorId,
		Title: entity.Title,
		PromptText: entity.PromptText,
		Tags: TagString.ToTags(entity.Tags),
		Visibility: PromptVisibilityMapper.ToModel(entity.Visibility),
		CreatedAt: entity.CreatedAt,
		UpdatedAt: entity.UpdatedAt,
		Likes: entity.Likes,
		Dislikes: entity.Dislikes,
		ETag: entity.ETag.ToString());

	public static PromptEntity FromModel(PromptModel model)
	{
		var now = DateTimeOffset.UtcNow;
		var title = model.Title.Trim();

		return new PromptEntity
		{
			PartitionKey = KeyFormat.PromptsPartitionKey(model.AuthorId),
			RowKey = model.PromptId,
			PromptId = model.PromptId,
			AuthorId = model.AuthorId,
			Title = title,
			TitleNormalized = title.ToLowerInvariant(),
			PromptText = model.PromptText,
			Tags = TagString.ToDelimited(model.Tags),
			Visibility = PromptVisibilityMapper.ToStorage(model.Visibility),
			CreatedAt = model.CreatedAt == default ? now : model.CreatedAt,
			UpdatedAt = model.UpdatedAt == default ? now : model.UpdatedAt,
			IsDeleted = false,
			Likes = model.Likes,
			Dislikes = model.Dislikes,
		};
	}
}
