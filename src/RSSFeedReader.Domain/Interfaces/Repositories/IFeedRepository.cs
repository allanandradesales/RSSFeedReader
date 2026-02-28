using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Domain.Interfaces.Repositories;

/// <summary>Persistence contract for <see cref="Feed"/> aggregates.</summary>
public interface IFeedRepository
{
    /// <summary>Returns all feed subscriptions ordered by <see cref="Feed.Title"/>.</summary>
    Task<IReadOnlyList<Feed>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the feed with the given ID, or <see langword="null"/> if not found.</summary>
    Task<Feed?> GetByIdAsync(Guid feedId, CancellationToken cancellationToken = default);

    /// <summary>Returns the feed whose URL matches <paramref name="url"/>, or <see langword="null"/>.</summary>
    Task<Feed?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Persists a new feed subscription.</summary>
    Task AddAsync(Feed feed, CancellationToken cancellationToken = default);

    /// <summary>Removes the feed with <paramref name="feedId"/> and cascades to its articles.</summary>
    Task DeleteAsync(Guid feedId, CancellationToken cancellationToken = default);

    /// <summary>Stamps <see cref="Feed.LastRefreshedAt"/> for the given feed.</summary>
    Task UpdateLastRefreshedAtAsync(Guid feedId, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default);
}
