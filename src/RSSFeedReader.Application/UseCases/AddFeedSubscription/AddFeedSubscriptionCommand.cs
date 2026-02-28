namespace RSSFeedReader.Application.UseCases.AddFeedSubscription;

/// <summary>Command to add a new RSS/Atom feed subscription by URL.</summary>
/// <param name="Url">The absolute HTTP/HTTPS URL of the feed.</param>
public sealed record AddFeedSubscriptionCommand(string Url);
