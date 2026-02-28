using RSSFeedReader.Domain.Interfaces.Repositories;

namespace RSSFeedReader.Application.UseCases.ToggleArticleReadStatus;

/// <summary>Result of executing <see cref="ToggleArticleReadStatusCommand"/>.</summary>
public sealed record ToggleArticleReadStatusResult(bool NewIsRead, int NewUnreadCount);

/// <summary>Handles <see cref="ToggleArticleReadStatusCommand"/>.</summary>
public sealed class ToggleArticleReadStatusHandler
{
    private readonly IArticleRepository _articleRepository;

    /// <summary>Initializes a new instance of <see cref="ToggleArticleReadStatusHandler"/>.</summary>
    public ToggleArticleReadStatusHandler(IArticleRepository articleRepository) =>
        _articleRepository = articleRepository;

    /// <summary>
    /// Toggles the article's read status and returns the new state plus the updated feed unread count.
    /// Returns <see langword="null"/> if the article was not found.
    /// </summary>
    public async Task<ToggleArticleReadStatusResult?> HandleAsync(
        ToggleArticleReadStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var newIsRead = await _articleRepository.ToggleReadStatusAsync(command.ArticleId, cancellationToken);
        if (newIsRead is null)
            return null;

        var newCount = await _articleRepository.GetUnreadCountByFeedIdAsync(command.FeedId, cancellationToken);
        return new ToggleArticleReadStatusResult(newIsRead.Value, newCount);
    }
}
