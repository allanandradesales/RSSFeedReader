using Moq;
using RSSFeedReader.Application.UseCases.MarkArticleAsRead;
using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.Tests.UseCases;

public sealed class MarkArticleAsReadHandlerTests
{
    private readonly Mock<IArticleRepository> _articleRepo = new();
    private readonly MarkArticleAsReadHandler _handler;

    public MarkArticleAsReadHandlerTests()
    {
        _handler = new MarkArticleAsReadHandler(_articleRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_MarksArticleAsRead_ReturnsNewUnreadCount()
    {
        var articleId = Guid.NewGuid();
        var feedId = Guid.NewGuid();

        _articleRepo.Setup(r => r.MarkAsReadAsync(articleId, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(2);

        var result = await _handler.HandleAsync(new MarkArticleAsReadCommand(articleId, feedId));

        Assert.Equal(2, result.NewUnreadCount);
        _articleRepo.Verify(r => r.MarkAsReadAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyRead_StillReturnsCurrentUnreadCount()
    {
        var articleId = Guid.NewGuid();
        var feedId = Guid.NewGuid();

        _articleRepo.Setup(r => r.MarkAsReadAsync(articleId, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _articleRepo.Setup(r => r.GetUnreadCountByFeedIdAsync(feedId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);

        var result = await _handler.HandleAsync(new MarkArticleAsReadCommand(articleId, feedId));

        Assert.Equal(0, result.NewUnreadCount);
    }
}
