namespace RSSFeedReader.Application.UseCases.GetArticlesByFeed;

/// <summary>Requests all articles for a given feed, sorted newest-first.</summary>
public sealed record GetArticlesByFeedQuery(Guid FeedId);
