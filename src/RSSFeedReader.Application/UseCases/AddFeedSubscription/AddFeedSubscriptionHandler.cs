using RSSFeedReader.Application.DTOs;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.UseCases.AddFeedSubscription;

/// <summary>Describes why adding a feed subscription failed.</summary>
public enum AddFeedSubscriptionError
{
    /// <summary>A subscription to this URL already exists.</summary>
    AlreadyExists,

    /// <summary>The feed could not be fetched or parsed. See <see cref="AddFeedSubscriptionResult.FetchError"/>.</summary>
    FetchFailed,
}

/// <summary>Result of executing <see cref="AddFeedSubscriptionCommand"/>.</summary>
public sealed record AddFeedSubscriptionResult
{
    /// <summary>Gets the newly added feed on success.</summary>
    public FeedDto? Feed { get; init; }

    /// <summary>Gets the error type on failure.</summary>
    public AddFeedSubscriptionError? Error { get; init; }

    /// <summary>Gets the underlying fetch error when <see cref="Error"/> is <see cref="AddFeedSubscriptionError.FetchFailed"/>.</summary>
    public FeedFetchError? FetchError { get; init; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a success result.</summary>
    public static AddFeedSubscriptionResult Ok(FeedDto feed) => new() { Feed = feed };

    /// <summary>Creates a failure result for a duplicate URL.</summary>
    public static AddFeedSubscriptionResult AlreadyExists() =>
        new() { Error = AddFeedSubscriptionError.AlreadyExists };

    /// <summary>Creates a failure result wrapping a fetch error.</summary>
    public static AddFeedSubscriptionResult FetchFailed(FeedFetchError fetchError) =>
        new() { Error = AddFeedSubscriptionError.FetchFailed, FetchError = fetchError };
}

/// <summary>Handles <see cref="AddFeedSubscriptionCommand"/>.</summary>
public sealed class AddFeedSubscriptionHandler
{
    private readonly IFeedRepository _feedRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IFeedFetcherService _fetcher;

    /// <summary>Initializes a new instance of <see cref="AddFeedSubscriptionHandler"/>.</summary>
    public AddFeedSubscriptionHandler(
        IFeedRepository feedRepository,
        IArticleRepository articleRepository,
        IFeedFetcherService fetcher)
    {
        _feedRepository = feedRepository;
        _articleRepository = articleRepository;
        _fetcher = fetcher;
    }

    /// <summary>Executes the command and returns a result discriminant.</summary>
    public async Task<AddFeedSubscriptionResult> HandleAsync(
        AddFeedSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await _feedRepository.GetByUrlAsync(command.Url, cancellationToken);
        if (existing is not null)
            return AddFeedSubscriptionResult.AlreadyExists();

        var fetchResult = await _fetcher.FetchAsync(command.Url, cancellationToken);
        if (!fetchResult.IsSuccess)
            return AddFeedSubscriptionResult.FetchFailed(fetchResult.Error);

        var feed = new Feed
        {
            Id = Guid.NewGuid(),
            Url = command.Url,
            Title = fetchResult.Value.Title,
            CreatedAt = DateTimeOffset.UtcNow,
            LastRefreshedAt = DateTimeOffset.UtcNow,
        };

        await _feedRepository.AddAsync(feed, cancellationToken);

        // Stamp the feedId on all articles then persist them
        var articles = fetchResult.Value.Articles;
        foreach (var article in articles)
            article.FeedId = feed.Id;

        if (articles.Count > 0)
            await _articleRepository.UpsertManyAsync(articles, cancellationToken);

        var dto = new FeedDto(feed.Id, feed.Url, feed.Title, feed.LastRefreshedAt, articles.Count);
        return AddFeedSubscriptionResult.Ok(dto);
    }
}
