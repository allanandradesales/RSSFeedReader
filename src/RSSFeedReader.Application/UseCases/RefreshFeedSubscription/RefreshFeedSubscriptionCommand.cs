namespace RSSFeedReader.Application.UseCases.RefreshFeedSubscription;

/// <summary>Instructs the handler to re-fetch articles for the given feed and return the new unread count.</summary>
public sealed record RefreshFeedSubscriptionCommand(Guid FeedId, string FeedUrl);
