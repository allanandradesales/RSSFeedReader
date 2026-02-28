namespace RSSFeedReader.Application.UseCases.ToggleArticleReadStatus;

/// <summary>Toggles the read status of the specified article.</summary>
public sealed record ToggleArticleReadStatusCommand(Guid ArticleId, Guid FeedId);
