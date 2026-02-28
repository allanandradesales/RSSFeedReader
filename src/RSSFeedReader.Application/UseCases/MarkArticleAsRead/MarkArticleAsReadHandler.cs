using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.UseCases.MarkArticleAsRead;

/// <summary>Result of executing <see cref="MarkArticleAsReadCommand"/>.</summary>
public sealed record MarkArticleAsReadResult(int NewUnreadCount);

/// <summary>Handles <see cref="MarkArticleAsReadCommand"/>.</summary>
public sealed class MarkArticleAsReadHandler
{
    private readonly IArticleRepository _articleRepository;

    /// <summary>Initializes a new instance of <see cref="MarkArticleAsReadHandler"/>.</summary>
    public MarkArticleAsReadHandler(IArticleRepository articleRepository) =>
        _articleRepository = articleRepository;

    /// <summary>Marks the article as read and returns the new unread count for its feed.</summary>
    public async Task<MarkArticleAsReadResult> HandleAsync(
        MarkArticleAsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        await _articleRepository.MarkAsReadAsync(command.ArticleId, cancellationToken);
        var newCount = await _articleRepository.GetUnreadCountByFeedIdAsync(command.FeedId, cancellationToken);
        return new MarkArticleAsReadResult(newCount);
    }
}
