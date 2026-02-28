using Moq;
using RSSFeedReader.Application.UseCases.ToggleArticleReadStatus;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class ToggleArticleReadStatusHandlerTests
{
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly ToggleArticleReadStatusHandler _handler;

    public ToggleArticleReadStatusHandlerTests()
    {
        _handler = new ToggleArticleReadStatusHandler(_articleRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_TogglesFromUnreadToRead_ReturnsNewIsReadTrueAndDecrementedCount()
    {
        var articleId = Guid.NewGuid();
        var feedId = Guid.NewGuid();

        _articleRepo.Setup(r => r.ToggleReadStatusAsync(articleId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(2);

        var result = await _handler.HandleAsync(new ToggleArticleReadStatusCommand(articleId, feedId));

        Assert.NotNull(result);
        Assert.True(result!.NewIsRead);
        Assert.Equal(2, result.NewUnreadCount);
    }

    [Fact]
    public async Task HandleAsync_TogglesFromReadToUnread_ReturnsNewIsReadFalseAndIncrementedCount()
    {
        var articleId = Guid.NewGuid();
        var feedId = Guid.NewGuid();

        _articleRepo.Setup(r => r.ToggleReadStatusAsync(articleId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(3);

        var result = await _handler.HandleAsync(new ToggleArticleReadStatusCommand(articleId, feedId));

        Assert.NotNull(result);
        Assert.False(result!.NewIsRead);
        Assert.Equal(3, result.NewUnreadCount);
    }

    [Fact]
    public async Task HandleAsync_ArticleNotFound_ReturnsNull()
    {
        var articleId = Guid.NewGuid();
        var feedId = Guid.NewGuid();

        _articleRepo.Setup(r => r.ToggleReadStatusAsync(articleId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((bool?)null);

        var result = await _handler.HandleAsync(new ToggleArticleReadStatusCommand(articleId, feedId));

        Assert.Null(result);
        _articleRepo.Verify(
            r => r.GetUnreadCountByFeedIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
