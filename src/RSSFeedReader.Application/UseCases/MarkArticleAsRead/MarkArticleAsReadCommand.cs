namespace RSSFeedReader.Application.UseCases.MarkArticleAsRead;

/// <summary>Marks the specified article as read.</summary>
public sealed record MarkArticleAsReadCommand(Guid ArticleId, Guid FeedId);
