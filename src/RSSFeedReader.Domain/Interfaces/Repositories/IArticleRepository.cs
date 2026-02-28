using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Domain.Interfaces.Repositories;

/// <summary>Persistence contract for <see cref="Article"/> records.</summary>
public interface IArticleRepository
{
    /// <summary>Returns all articles sorted newest-first.</summary>
    Task<IReadOnlyList<Article>> GetAllSortedAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns articles for a specific feed sorted newest-first.</summary>
    Task<IReadOnlyList<Article>> GetByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of unread articles for a specific feed.</summary>
    Task<int> GetUnreadCountByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts articles that do not yet exist (matched by <see cref="Article.FeedGuid"/>) and
    /// updates those that do. Existing <see cref="Article.IsRead"/> state is preserved on update.
    /// </summary>
    Task UpsertManyAsync(IEnumerable<Article> articles, CancellationToken cancellationToken = default);

    /// <summary>Sets <see cref="Article.IsRead"/> to <see langword="true"/> for the specified article.</summary>
    Task MarkAsReadAsync(Guid articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles <see cref="Article.IsRead"/> for the specified article and returns the new value,
    /// or <see langword="null"/> if the article was not found.
    /// </summary>
    Task<bool?> ToggleReadStatusAsync(Guid articleId, CancellationToken cancellationToken = default);

    /// <summary>Deletes all articles belonging to the specified feed.</summary>
    Task DeleteByFeedIdAsync(Guid feedId, CancellationToken cancellationToken = default);
}
