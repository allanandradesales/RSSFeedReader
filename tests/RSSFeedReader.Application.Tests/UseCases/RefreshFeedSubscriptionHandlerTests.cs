using Moq;
using RSSFeedReader.Application.UseCases.RefreshFeedSubscription;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class RefreshFeedSubscriptionHandlerTests
{
    private readonly Mock<IFeedRepository> _feedRepo = new();
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly Mock<IFeedFetcherService> _fetcher = new();
    private readonly RefreshFeedSubscriptionHandler _handler;

    public RefreshFeedSubscriptionHandlerTests()
    {
        _handler = new RefreshFeedSubscriptionHandler(_feedRepo.Object, _articleRepo.Object, _fetcher.Object);
    }

    [Fact]
    public async Task HandleAsync_FeedNotFound_ReturnsNotFoundError()
    {
        _feedRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Feed?)null);

        var result = await _handler.HandleAsync(
            new RefreshFeedSubscriptionCommand(Guid.NewGuid(), "https://example.com/feed"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshFeedError.FeedNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_FetchFails_ReturnsFetchFailedError()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Ex" };
        _feedRepo.Setup(r => r.GetByIdAsync(feed.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(feed);
        _fetcher.Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.HttpError));

        var result = await _handler.HandleAsync(
            new RefreshFeedSubscriptionCommand(feed.Id, feed.Url));

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshFeedError.FetchFailed, result.Error);
        Assert.Equal(FeedFetchError.HttpError, result.FetchError);
    }

    [Fact]
    public async Task HandleAsync_Success_UpsertsArticlesAndReturnsNewUnreadCount()
    {
        var feedId = Guid.NewGuid();
        var feed = new Feed { Id = feedId, Url = "https://example.com/feed", Title = "My Feed" };
        var articles = new List<Article>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FeedGuid = "guid-1",
                Title = "New Article",
                OriginalUrl = "https://example.com/1",
                PublishedAt = DateTimeOffset.UtcNow,
                FetchedAt = DateTimeOffset.UtcNow,
            }
        };

        _feedRepo.Setup(r => r.GetByIdAsync(feedId, It.IsAny<CancellationToken>())).ReturnsAsync(feed);
        _fetcher.Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok<FeedFetchResult, FeedFetchError>(FeedFetchResult.Success("My Feed", articles)));
        _articleRepo.Setup(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Article>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _feedRepo.Setup(r => r.UpdateLastRefreshedAtAsync(feedId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(7);

        var result = await _handler.HandleAsync(new RefreshFeedSubscriptionCommand(feedId, feed.Url));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.NewUnreadCount);
        Assert.NotNull(result.NewLastRefreshedAt);
        _articleRepo.Verify(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Article>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
