using Moq;
using RSSFeedReader.Application.UseCases.GetFeeds;
using RSSFeedReader.Domain.Entities;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class GetFeedsHandlerTests
{
    private readonly Mock<IFeedRepository> _feedRepo = new();
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly GetFeedsHandler _handler;

    public GetFeedsHandlerTests()
    {
        _handler = new GetFeedsHandler(_feedRepo.Object, _articleRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_NoFeeds_ReturnsEmptyList()
    {
        _feedRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        var result = await _handler.HandleAsync(new GetFeedsQuery());

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_WithFeeds_MapsUnreadCount()
    {
        var feedId = Guid.NewGuid();
        var feeds = new List<Feed>
        {
            new() { Id = feedId, Url = "https://example.com/feed", Title = "Feed A", CreatedAt = DateTimeOffset.UtcNow }
        };
        _feedRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feeds);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>())).ReturnsAsync(5);

        var result = await _handler.HandleAsync(new GetFeedsQuery());

        Assert.Single(result);
        Assert.Equal(5, result[0].UnreadCount);
        Assert.Equal("Feed A", result[0].Title);
    }
}
