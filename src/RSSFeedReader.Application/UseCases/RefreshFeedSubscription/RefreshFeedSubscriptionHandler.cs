using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.UseCases.RefreshFeedSubscription;

/// <summary>Describes why refreshing a feed subscription failed.</summary>
public enum RefreshFeedError
{
    /// <summary>No feed with the given ID exists in the database.</summary>
    FeedNotFound,

    /// <summary>The feed fetcher returned an error.</summary>
    FetchFailed,
}

/// <summary>Result of executing <see cref="RefreshFeedSubscriptionCommand"/>.</summary>
public sealed record RefreshFeedSubscriptionResult
{
    /// <summary>Gets the new unread article count for the feed on success.</summary>
    public int NewUnreadCount { get; init; }

    /// <summary>Gets the timestamp of the refresh on success.</summary>
    public DateTimeOffset? NewLastRefreshedAt { get; init; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Gets the error type on failure.</summary>
    public RefreshFeedError? Error { get; init; }

    /// <summary>Gets the underlying fetch error when <see cref="Error"/> is <see cref="RefreshFeedError.FetchFailed"/>.</summary>
    public FeedFetchError? FetchError { get; init; }

    /// <summary>Creates a success result.</summary>
    public static RefreshFeedSubscriptionResult Ok(int count, DateTimeOffset refreshedAt) =>
        new() { NewUnreadCount = count, NewLastRefreshedAt = refreshedAt };

    /// <summary>Creates a failure result when the feed is not found.</summary>
    public static RefreshFeedSubscriptionResult NotFound() =>
        new() { Error = RefreshFeedError.FeedNotFound };

    /// <summary>Creates a failure result wrapping a fetch error.</summary>
    public static RefreshFeedSubscriptionResult Fail(FeedFetchError fetchError) =>
        new() { Error = RefreshFeedError.FetchFailed, FetchError = fetchError };
}

/// <summary>Handles <see cref="RefreshFeedSubscriptionCommand"/>.</summary>
public sealed class RefreshFeedSubscriptionHandler
{
    private readonly IFeedRepository _feedRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IFeedFetcherService _fetcher;

    /// <summary>Initializes a new instance of <see cref="RefreshFeedSubscriptionHandler"/>.</summary>
    public RefreshFeedSubscriptionHandler(
        IFeedRepository feedRepository,
        IArticleRepository articleRepository,
        IFeedFetcherService fetcher)
    {
        _feedRepository = feedRepository;
        _articleRepository = articleRepository;
        _fetcher = fetcher;
    }

    /// <summary>Executes the command and returns a result discriminant.</summary>
    public async Task<RefreshFeedSubscriptionResult> HandleAsync(
        RefreshFeedSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var feed = await _feedRepository.GetByIdAsync(command.FeedId, cancellationToken);
        if (feed is null)
            return RefreshFeedSubscriptionResult.NotFound();

        var fetchResult = await _fetcher.FetchAsync(command.FeedUrl, cancellationToken);
        if (!fetchResult.IsSuccess)
            return RefreshFeedSubscriptionResult.Fail(fetchResult.Error);

        var articles = fetchResult.Value.Articles;
        foreach (var article in articles)
            article.FeedId = command.FeedId;

        if (articles.Count > 0)
            await _articleRepository.UpsertManyAsync(articles, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        await _feedRepository.UpdateLastRefreshedAtAsync(command.FeedId, utcNow, cancellationToken);

        var newUnreadCount = await _articleRepository.GetUnreadCountByFeedIdAsync(command.FeedId, cancellationToken);
        return RefreshFeedSubscriptionResult.Ok(newUnreadCount, utcNow);
    }
}
