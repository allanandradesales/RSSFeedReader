using Moq;
using RSSFeedReader.Application.UseCases.AddFeedSubscription;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class AddFeedSubscriptionHandlerTests
{
    private readonly Mock<IFeedRepository> _feedRepo = new();
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly Mock<IFeedFetcherService> _fetcher = new();
    private readonly AddFeedSubscriptionHandler _handler;

    public AddFeedSubscriptionHandlerTests()
    {
        _handler = new AddFeedSubscriptionHandler(_feedRepo.Object, _articleRepo.Object, _fetcher.Object);
    }

    [Fact]
    public async Task HandleAsync_AlreadySubscribed_ReturnsAlreadyExistsError()
    {
        var existingFeed = new Feed { Url = "https://example.com/feed", Title = "Ex" };
        _feedRepo.Setup(r => r.GetByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingFeed);

        var result = await _handler.HandleAsync(new AddFeedSubscriptionCommand("https://example.com/feed"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddFeedSubscriptionError.AlreadyExists, result.Error);
    }

    [Fact]
    public async Task HandleAsync_FetchFails_ReturnsFetchFailedError()
    {
        _feedRepo.Setup(r => r.GetByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Feed?)null);
        _fetcher.Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Fail<FeedFetchResult, FeedFetchError>(FeedFetchError.Timeout));

        var result = await _handler.HandleAsync(new AddFeedSubscriptionCommand("https://example.com/feed"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddFeedSubscriptionError.FetchFailed, result.Error);
        Assert.Equal(FeedFetchError.Timeout, result.FetchError);
    }

    [Fact]
    public async Task HandleAsync_NewFeed_AddsAndReturnsDto()
    {
        _feedRepo.Setup(r => r.GetByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Feed?)null);

        var articles = new List<Article>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FeedGuid = "guid-1",
                Title = "Article 1",
                OriginalUrl = "https://example.com/1",
                PublishedAt = DateTimeOffset.UtcNow,
                FetchedAt = DateTimeOffset.UtcNow,
            }
        };

        _fetcher.Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok<FeedFetchResult, FeedFetchError>(
                    FeedFetchResult.Success("My Feed", articles)));

        _feedRepo.Setup(r => r.AddAsync(It.IsAny<Feed>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _articleRepo.Setup(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Article>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(new AddFeedSubscriptionCommand("https://example.com/feed"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Feed);
        Assert.Equal("My Feed", result.Feed!.Title);
        Assert.Equal(1, result.Feed.UnreadCount);
    }
}
